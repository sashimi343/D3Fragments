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

## Mapping value objects

Map value object properties as EF Core complex types with `ComplexValueObject`.
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
- @D3Fragments.EFCore.ValueObjectComplexTypeExtensions
- @D3Fragments.EFCore.D3FragmentsDbContextOptionsBuilderExtensions
- @D3Fragments.EFCore.D3FragmentsEFCoreServiceCollectionExtensions
- <xref:D3Fragments.EFCore.EfUnitOfWork`1>
