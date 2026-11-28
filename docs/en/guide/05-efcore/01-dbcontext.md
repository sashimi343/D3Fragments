# DbContext and mapping

## Deriving from D3DbContext

The application's `DbContext` derives from `D3DbContext`.
Pass the `DbContextOptions` to the base class in the constructor.

```csharp
using D3Fragments.EFCore;
using Microsoft.EntityFrameworkCore;

public sealed class LibraryDbContext(DbContextOptions options) : D3DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Member> Members => Set<Member>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(builder =>
        {
            builder.ComplexValueObject(x => x.Isbn);
            builder.ComplexValueObject(x => x.Title);
        });
        modelBuilder.Entity<Member>(builder =>
        {
            builder.ComplexValueObject(x => x.Name);
            builder.ComplexValueObject(x => x.EmailAddress);
        });
    }
}
```

`D3DbContext` applies the following configuration automatically.

| Target | Configuration |
|---|---|
| `UuidV7` properties | A value converter that stores the value as a `Guid` |
| Type-safe enum properties | A value converter that stores the key (for the types in the assemblies passed to `UseD3Fragments`) |
| `Version` of `Entity` and `AggregateRoot` | The optimistic concurrency token |
| `UpdatedAt` / `Version` of `Entity` and `AggregateRoot` | Updated when changes are saved |

If you override `ConfigureConventions`, call `base.ConfigureConventions`.
Otherwise, the configuration above is not applied.

## When you cannot derive from D3DbContext

A `DbContext` that derives from another base class, such as `IdentityDbContext`, cannot derive from `D3DbContext`.
In that case, call `ApplyD3FragmentsConventions` in `ConfigureConventions`.

```csharp
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<Account, IdentityRole<Guid>, Guid>(options)
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ApplyD3FragmentsConventions(options);
    }
}
```

`ApplyD3FragmentsConventions` sets the same value converters and concurrency tokens as `D3DbContext`, and detects unmapped value objects.
Pass the `DbContextOptions` received by the constructor (it reads the assemblies to search for type-safe enums from the `UseD3Fragments` settings).

Updating `UpdatedAt` / `Version` on save is done by the interceptor that `UseD3Fragments` (called by `AddD3FragmentsEFCore`) registers.
If you configure the context with `UseD3Fragments` but forget to call `ApplyD3FragmentsConventions`, an `InvalidOperationException` is thrown on save.

See [ASP.NET Core Identity integration](../07-identity/01-setup.md) for how to combine this with Identity.

## Mapping value objects

There are two ways to map value object properties.
Use `ComplexValueObject` in most cases, and use `HasValueObjectConversion` only for properties that need an index, such as a unique constraint.

| | `ComplexValueObject` (usual) | `HasValueObjectConversion` |
|---|---|---|
| Mapping | Complex type | Value converter |
| When to use | Value objects without an index | Value objects with a unique constraint or an index |
| Index | Cannot be defined | Can be defined |
| Using the wrapped value in queries (`x.Email.Value.EndsWith(...)`) | Supported | Not supported (compare value objects, such as `x.Email == email`) |
| Column name | Follows the naming convention | Follows the naming convention |

### Mapping as a complex type

`ComplexValueObject` maps a value object property as an EF Core complex type.
By default, a value object is stored in a single column named after the property (`Email`, not `Email_Value`).

```csharp
modelBuilder.Entity<Member>(builder =>
{
    builder.ComplexValueObject(x => x.EmailAddress)
        .Property(x => x.Value).HasMaxLength(254);
});
```

You can change the column name, length, and so on with the returned builder.

With complex type mapping, you can use the wrapped value directly in queries.

```csharp
var members = await DbContext.Members
    .Where(x => x.EmailAddress.Value.EndsWith("@example.com"))
    .ToListAsync(cancellationToken);
```

As with `HasValueObjectConversion`, the column name follows the naming convention of the DbContext.
For example, with `UseSnakeCaseNamingConvention()` of EFCore.NamingConventions, the column name is `email_address`, not `email_address_value`.
This is because the trailing `_value` (case-insensitive) is removed from the column name converted by the naming convention.
So with a custom naming convention that changes the separator, the suffix is not removed and the converted column name is kept as is.
In that case, specify the column name with the returned builder.

```csharp
builder.ComplexValueObject(x => x.EmailAddress)
    .Property(x => x.Value).HasColumnName("email-address");
```

### Mapping with a value converter

Map properties that need an index, such as a unique constraint, with a value converter by using `HasValueObjectConversion`.
This is because EF Core cannot define an index on a complex type property.
You do not need to specify the wrapped type (such as `string`); it is determined from the value object type.

```csharp
modelBuilder.Entity<Member>(builder =>
{
    builder.Property(x => x.EmailAddress).HasValueObjectConversion().HasMaxLength(254);
    builder.HasIndex(x => x.EmailAddress).IsUnique();
});
```

The column name is not set, so it follows the naming convention of the DbContext (`email_address` with `UseSnakeCaseNamingConvention()`).
When a value is read from the database, the value object is created with `Create`, so a value that fails `ValidateValue` throws an exception.

In queries, compare value objects instead of the wrapped values.

```csharp
var member = await DbContext.Members
    .SingleOrDefaultAsync(x => x.EmailAddress == emailAddress, cancellationToken);
```

When the unique constraint is violated, `DbUpdateException` is thrown on save.

### Mapping a collection of value objects

A collection of value objects, such as `List<Email>`, can be mapped in two ways, like a single value object.
Both store the whole collection in a single JSON column named after the property.
Use `ComplexValueObjectCollection` in most cases, and use `HasValueObjectCollectionConversion` when the column should be an array of only the wrapped values (for example, to match an existing schema).

| | `ComplexValueObjectCollection` (usual) | `HasValueObjectCollectionConversion` |
|---|---|---|
| Mapping | Complex collection | Value converter for the elements |
| Stored JSON | `[{"Value":"a@example.com"}]` | `["a@example.com"]` |
| Comparing value objects (`x.Aliases.Contains(email)`) | Supported | Supported |
| Using the wrapped value in queries (`x.Aliases.Any(a => a.Value.EndsWith(...))`) | Supported | Not supported |

```csharp
public sealed class Mailbox : AggregateRoot
{
    private readonly List<Email> _aliases = [];

    public IReadOnlyList<Email> Aliases => _aliases;
}

modelBuilder.Entity<Mailbox>(builder =>
{
    builder.ComplexValueObjectCollection(x => x.Aliases);
    // or
    // builder.PrimitiveCollection(x => x.Aliases).HasValueObjectCollectionConversion();
});
```

You can expose the collection as a read-only type such as `IReadOnlyList<T>`.
In that case, EF Core reads and writes the backing field instead of the property.

- The backing field is found by the EF Core naming convention (`_aliases`, `_Aliases`, `m_aliases`, and so on for a property named `Aliases`). If the field does not follow the convention and the property has no setter, an `InvalidOperationException` is thrown when the mapping is configured.
- With `ComplexValueObjectCollection`, make the backing field a `List<T>`.

With either method, each element is validated when read from the database, and a value that fails `ValidateValue` throws an exception.

### When you forget to map a value object

If you forget to map a value object, EF Core tries to treat it as an entity.
`D3DbContext` detects this and throws an `InvalidOperationException` that names the unmapped properties when the model is built.

> [!NOTE]
> An entity with value object properties needs a parameterless constructor that does not take value objects (it can be `private`).
> This is because EF Core cannot pass complex type properties to constructor parameters.

## Optimistic concurrency control

`Version` of `Entity` and `AggregateRoot` is treated as a concurrency token.
If another operation has already updated the same aggregate, EF Core detects the conflict when saving.
The Unit of Work converts this into a `ConflictOccurredException` (`ErrorType.Conflict`) and throws it.

Even when only a child entity is changed, the aggregate root's `Version` increases.
As a result, conflicts are detected even when different parts of an aggregate are changed at the same time.
When a child entity is moved to another aggregate, the `Version` of both the source and the destination aggregate roots increases.

## UseD3Fragments

When you configure a `DbContext` without `AddD3FragmentsEFCore` (for example, in tests), pass the assemblies that define type-safe enums to `UseD3Fragments`.

```csharp
var options = new DbContextOptionsBuilder<LibraryDbContext>()
    .UseNpgsql(connectionString)
    .UseD3Fragments([typeof(Book).Assembly])
    .Options;
```

## Related APIs

- @D3Fragments.EFCore.D3DbContext
- @D3Fragments.EFCore.D3FragmentsModelConfigurationBuilderExtensions
- @D3Fragments.EFCore.ValueObjectComplexTypeExtensions
- @D3Fragments.EFCore.ValueObjectConversionExtensions
- @D3Fragments.EFCore.D3FragmentsDbContextOptionsBuilderExtensions
- @D3Fragments.EFCore.D3FragmentsEFCoreServiceCollectionExtensions
- <xref:D3Fragments.EFCore.EfUnitOfWork`1>
