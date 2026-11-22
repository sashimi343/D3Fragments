# Setup

## Projects and packages

The packages added for the Identity integration are as follows.

| Layer | Packages to add |
|---|---|
| Infrastructure layer | `Sashimi343.D3Fragments.Identity.EFCore`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore` |
| Presentation layer | None (the cookie-side extension methods are included in `Sashimi343.D3Fragments.AspNetCore`) |

Do not add Identity packages to the application layer or the domain layer.

## The user type

Put the Identity user type (a class derived from `IdentityUser<TKey>`) in the infrastructure layer.
It holds data for authentication, such as the user name, the password hash, and the two-factor authentication settings.

```csharp
public sealed class UserAccount : IdentityUser<UuidV7>
{
    public UserAccount()
    {
        Id = UuidV7.NewUuidV7();
        SecurityStamp = Guid.NewGuid().ToString();
    }
}
```

The presentation layer references the user type as the type argument of `SignInManager<TUser>` and to issue cookies.
References from the presentation layer to infrastructure layer types are forbidden by [DFR0002](../../analyzers/DFR0002.md), but the user type is allowed as an exception (see `Infrastructure_IdentityUser` in [Architecture](../01-architecture.md)).

### Separating the business user

Business data such as a nickname or the date the user joined is not added to the user type; define it as an aggregate in the domain layer.
The aggregate holds the ID of the Identity user.

```csharp
public sealed class Member : AggregateRoot
{
    // ID of the Identity user
    public UuidV7 AccountId { get; private set; }

    public Nickname Nickname { get; private set; }

    public DateOnly JoinedOn { get; private set; }
    // ...
}
```

Put the user and the aggregate in the same `DbContext`, and register them in the same Command.
`UnitOfWorkBehavior` saves them together, so one is never saved without the other.

```csharp
public async Task<Result<RegisteredAccount>> HandleAsync(RegisterAccountCommand request, CancellationToken cancellationToken)
{
    // The UnitOfWork saves even when a failed Result is returned, so validate the member input before creating the Identity user
    Result<Nickname> nickname = Nickname.TryCreate(request.Nickname);
    if (nickname.IsFailure)
    {
        return Result<RegisteredAccount>.Failure(nickname.Errors);
    }

    Result<RegisteredAccount> account = await accounts.RegisterAsync(request.UserName, request.EmailAddress, request.Password);
    if (account.IsSuccess)
    {
        members.Add(Member.TryRegister(account.Value!.AccountId, request.Nickname, timeProvider.GetLocalToday()).GetValueOrThrow());
    }
    return account;
}
```

`accounts` is an interface defined by the application, implemented in the infrastructure layer with `UserManager<TUser>`.

> [!WARNING]
> `UnitOfWorkBehavior` saves the changes even when the handler returns a failed `Result`.
> If you return a failure as a `Result` after creating the user, only the user is saved.
> Finish validating the input before creating the user, and throw an exception for failures after the user is created (on an exception, the changes are discarded).

## DbContext

A `DbContext` that holds the Identity tables derives from `IdentityDbContext` (when roles are used) or `IdentityUserContext` (when roles are not used).
Therefore it cannot derive from `D3DbContext`.
Instead, call `ApplyD3FragmentsConventions` in `ConfigureConventions`.

```csharp
public sealed class IdentityIntegrationDbContext(DbContextOptions<IdentityIntegrationDbContext> options)
    : IdentityUserContext<UserAccount, UuidV7>(options)
{
    public DbSet<Member> Members => Set<Member>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyD3FragmentsConventions(options);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Definitions of the Identity tables
        base.OnModelCreating(builder);

        builder.Entity<Member>(member =>
        {
            member.ComplexValueObject(x => x.Nickname);
            member.HasIndex(x => x.AccountId).IsUnique();
        });
    }
}
```

`ApplyD3FragmentsConventions` applies the same conventions as `D3DbContext` (value converters for `UuidV7` and type-safe enums, `Version` as a concurrency token, and detection of unmapped value objects).
Updating `UpdatedAt` and `Version` on save is done by the interceptor that `AddD3FragmentsEFCore` (which calls `UseD3Fragments` internally) registers.
If you forget to call `ApplyD3FragmentsConventions` on a context configured with `UseD3Fragments`, an `InvalidOperationException` is thrown on save.
See [DbContext and mapping](../05-efcore/01-dbcontext.md) for details.

## Service registration

In the infrastructure layer, use `AddIdentityCore` instead of `AddIdentity`, and call `AddD3FragmentsStores` instead of `AddEntityFrameworkStores`.

```csharp
services.AddD3FragmentsEFCore<IdentityIntegrationDbContext>(
    [typeof(IdentityIntegrationDbContext).Assembly],
    options => options.UseSqlite(connectionString));

services.AddIdentityCore<UserAccount>(options =>
    {
        options.SignIn.RequireConfirmedEmail = true;
        options.User.RequireUniqueEmail = true;
        // Create the passkey table (AspNetUserPasskeys)
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    })
    .AddSignInManager()
    .AddDefaultTokenProviders()
    // Stores that leave saving to the UnitOfWork
    .AddD3FragmentsStores<IdentityIntegrationDbContext>();
```

In the presentation layer, register the Identity cookies.

```csharp
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();
```

`AddIdentityCookies` registers four cookies: the application cookie, the external login cookie, the two-factor pending cookie, and the "remember this browser" cookie.
`AddIdentity` also registers the cookies, so use `AddIdentityCore` to be able to register the cookies in the presentation layer.

### AddD3FragmentsStores

`AddD3FragmentsStores<TContext>` registers a user store (and a role store when roles are used) that leaves saving to the Unit of Work.
`UserManager<TUser>` operations only change tracked entities, and the Command's `UnitOfWorkBehavior` saves the changes together with the domain changes.

| Item | Contents |
|---|---|
| Registered stores | `D3UserStore` and `D3RoleStore` when roles are used, otherwise `D3UserOnlyStore` |
| Type arguments of the stores | Determined from the base type of `TContext` (`IdentityDbContext` or `IdentityUserContext`), in the same way as `AddEntityFrameworkStores` |
| Lifetime | Scoped |
| Already registered stores | Replaced (for example, those registered by `AddEntityFrameworkStores`) |
| Registration of `TContext` | Not performed. Register it with `AddD3FragmentsEFCore` |

Unlike the other D3Fragments registration methods, `AddD3FragmentsStores` replaces already registered stores.
It throws an `InvalidOperationException` if the user type does not derive from `IdentityUser<TKey>` or the role type does not derive from `IdentityRole<TKey>`.

If you use passkeys, set `IdentitySchemaVersions.Version3`.
Otherwise the passkey table is not created, and an exception occurs the first time a passkey is used.

## Converting IdentityResult into Result

`UserManager<TUser>` operations return an `IdentityResult`.
Converting it into a D3Fragments `Result` with `ToResult` lets you handle errors in the same way as in other Commands.

```csharp
Result created = (await users.CreateAsync(account, password)).ToResult();
if (created.IsFailure)
{
    return Result<RegisteredAccount>.Failure(created.Errors);
}
```

Each `IdentityError` becomes an `Error` whose message is `Description` and whose code is `Code`.
The error types are as follows.

| `IdentityError.Code` | `ErrorType` |
|---|---|
| `DuplicateUserName`, `DuplicateEmail`, `DuplicateRoleName`, `LoginAlreadyAssociated`, `ConcurrencyFailure` | `Conflict` |
| `UserAlreadyHasPassword`, `UserAlreadyInRole`, `UserNotInRole`, `UserLockoutNotEnabled` | `InvalidState` |
| Others | `Validation` |

For a different mapping, pass a conversion function to `ToResult(map)`.

```csharp
return (await users.ConfirmEmailAsync(account, token))
    .ToResult(_ => Error.Validation("The confirmation link is invalid or has expired."));
```

## Related APIs

- @D3Fragments.Identity.EFCore.D3FragmentsIdentityBuilderExtensions
- @D3Fragments.Identity.EFCore.IdentityResultExtensions
- @D3Fragments.EFCore.D3FragmentsModelConfigurationBuilderExtensions
