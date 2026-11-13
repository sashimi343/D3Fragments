# EF Core integration

The `Sashimi343.D3Fragments.EFCore` package provides features for persisting the D3Fragments domain model with EF Core.
Use it in the infrastructure layer.

| Page | Main types |
|---|---|
| [DbContext and mapping](01-dbcontext.md) | `D3DbContext`, `ComplexValueObject`, `AddD3FragmentsEFCore` |
| [Repositories and paging](02-repositories.md) | `RepositoryBase<TContext, TAggregate>`, `ToPagedListAsync`, `Pager<T>` |

## Service registration

Pass the following two to `AddD3FragmentsEFCore`:

- The domain layer assembly and the assembly that contains the repository implementations
- A delegate that configures the database provider

```csharp
builder.Services.AddD3FragmentsEFCore<LibraryDbContext>(
    [typeof(Book).Assembly, typeof(BookRepository).Assembly],
    options => options.UseNpgsql(connectionString));
```

The `DbContext`, the `IUnitOfWork` implementation, and the repository implementations in the assemblies you pass are registered.
See [Service registration](../02-service-registration.md) for the list of registered services.
