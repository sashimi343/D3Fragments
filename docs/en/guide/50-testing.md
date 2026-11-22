# Testing

This page describes a testing strategy for applications that use D3Fragments and how to use the testing packages.

## Packages

| Package | Contents | Main dependencies |
|---|---|---|
| `Sashimi343.D3Fragments.Testing` | `ResultAssert`, `DomainEventAssert`, `SpyDomainEventDispatcher`, `TestData`, `TestTime` | xunit.v3.assert, Microsoft.Extensions.TimeProvider.Testing |
| `Sashimi343.D3Fragments.Testing.EFCore` | `DatabaseContainerFixture`, `TransactionalTestBase` | Sashimi343.D3Fragments.EFCore, Testcontainers, xunit.v3.extensibility.core |

Reference DBMS-specific packages (such as `Testcontainers.PostgreSql` and `Npgsql.EntityFrameworkCore.PostgreSQL`) from the application's test projects.

## Testing strategy by layer

| Target | Approach | Packages |
|---|---|---|
| Domain (aggregates, value objects, domain rules) | Unit tests without a database | Testing |
| Handlers (Commands and Queries) | A real database, with the transaction rolled back after each test | Testing, Testing.EFCore |
| Repositories | A real database, with the transaction rolled back after each test | Testing, Testing.EFCore |
| E2E | Keep the number small | As needed |

- Repositories also implement complex reads, so do not create fakes for them. Handler tests also use the real repositories against a real database.
- Only one database container is started for the whole test assembly. The cost per test is just starting and rolling back a transaction.
- For domain events, verify only that they were raised. Verify domain event handlers in separate tests.

### Example test project structure

```text
Xxx.Domain.Tests         → Xxx.Domain, Sashimi343.D3Fragments.Testing
Xxx.Application.Tests    → Xxx.Tests.Helpers
Xxx.Infrastructure.Tests → Xxx.Tests.Helpers
Xxx.Tests.Helpers        → Xxx.Infrastructure, Sashimi343.D3Fragments.Testing.EFCore, Testcontainers.PostgreSql, etc.
                           (contains the fixture and test data helpers)
```

Because handler tests use a real database, `Xxx.Application.Tests` depends on `Xxx.Infrastructure`.
This differs from the dependency direction of the production code, but it is acceptable for tests.

## Setup

### 1. Define a fixture

Derive from `DatabaseContainerFixture<TDbContext>` and define how to create the database container and the `DbContext`.

```csharp
public sealed class AppDbFixture : DatabaseContainerFixture<AppDbContext>
{
    protected override IDatabaseContainer CreateContainer() =>
        new PostgreSqlBuilder("postgres:18-alpine").Build();

    public override AppDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(ConnectionString)
            .UseD3Fragments([typeof(Order).Assembly])
            .Options);
}
```

By default, the schema is created with `EnsureCreatedAsync`.
To apply migrations instead, override `InitializeDatabaseAsync`.

### 2. Register it as an assembly fixture

Write this in any one file in the test project.
This makes the container start only once for the whole test assembly.

```csharp
[assembly: AssemblyFixture(typeof(AppDbFixture))]
```

### 3. Create a base class for your application (optional)

```csharp
public abstract class AppDbTestBase(AppDbFixture fixture)
    : TransactionalTestBase<AppDbContext, AppDbFixture>(fixture);
```

## Testing handlers

```csharp
public sealed class PlaceOrderHandlerTests(AppDbFixture fixture) : AppDbTestBase(fixture)
{
    [Fact]
    public async Task CanPlaceOrder()
    {
        var customer = CustomerTestFactory.Create();
        await SeedAsync(customer);
        var handler = new PlaceOrderHandler(new OrderRepository(DbContext), new CustomerRepository(DbContext));

        var orderId = await ExecuteAsync(handler, new PlaceOrderCommand(customer.Id));

        await AssertEntityExistsAsync<Order>(x => x.Id == orderId);
        DomainEventAssert.Raised<OrderPlaced>(DispatchedEvents);
    }
}
```

- Always create repositories by passing `DbContext`. Other `DbContext` instances cannot see the data inside the transaction.
- `ExecuteAsync` runs the handler and returns the handler's response. For a Command, it then saves with `UnitOfWork.SaveChangesAsync` and clears the change tracker. Because this goes through the same save process as `UnitOfWorkBehavior` in production, the raised domain events are recorded in `DispatchedEvents`.
- `TransactionBehavior` is not applied, because the test itself runs inside a transaction. If a handler calls `IUnitOfWork.ExecuteInTransactionAsync` directly, the transactions are nested and it fails.
- If a handler needs `IUnitOfWork`, pass the base class's `UnitOfWork` property.
- To verify the saved state in detail, retrieve it again through a repository. After `ExecuteAsync`, the change tracker has been cleared, so the data is read from the database.

## Testing the domain

Do not use a database.
Verify the domain events recorded by an aggregate by passing the aggregate to `DomainEventAssert`.

```csharp
[Fact]
public void CancelingAnOrderRaisesAnEvent()
{
    var order = OrderTestFactory.CreatePlaced();

    order.Cancel(TestTime.Now);

    DomainEventAssert.Raised<OrderCanceled>(order);
}

[Fact]
public void EmptyTitleIsAValidationError()
{
    var result = BookTitle.TryCreate("");

    ResultAssert.HasError(result, ErrorType.Validation);
}
```

## Preparing test data

How to create test data differs from domain to domain, so create a generation helper (Object Mother) for each aggregate in the application.
Follow these conventions.

1. Create an `XxxTestFactory` for each aggregate. Make all parameters optional with reasonable defaults. In tests, specify only the values relevant to what is being verified.
2. Create objects with their related aggregates and entities already connected, so that they can be registered with a single `SeedAsync` call.
3. Generate values with unique constraints with `TestData.Unique("DEV")` (`DEV-1`, `DEV-2`, ...). This prevents collisions between tests running in parallel.
4. Use `TestTime.Now` for times that have business meaning (such as the order time). Where a `TimeProvider` is needed, pass `TestTime.CreateProvider()`. `CreatedAt` / `UpdatedAt` are recorded automatically from the system time and cannot be fixed. To verify them, check a range (`Assert.InRange`).
5. Create aggregates in specific states (canceled, returned, and so on) with methods such as `CreateCanceled()` that call the domain methods in order. Do not rewrite internal state with reflection, because you would end up testing with data that violates the invariants.

```csharp
public static class OrderTestFactory
{
    public static Order CreatePlaced(Customer? customer = null, int quantity = 1) =>
        Order.Place(customer ?? CustomerTestFactory.Create(), quantity, TestTime.Now);

    public static Order CreateCanceled(Customer? customer = null)
    {
        var order = CreatePlaced(customer);
        order.Cancel(TestTime.Now);
        return order;
    }
}
```

`SeedAsync` accepts entities of different types together.
Domain events recorded by the registered entities are discarded without being dispatched (because the entities are treated as existing data).

```csharp
await SeedAsync(customer, product, order);
await SeedAsync(orders); // An IEnumerable can be passed as is
```

## Notes on xUnit v3

- Test projects need `<OutputType>Exe</OutputType>`.
- `dotnet test` runs on the Microsoft Testing Platform. Add the following to `global.json`:

  ```json
  "test": { "runner": "Microsoft.Testing.Platform" }
  ```

- Methods that take a `CancellationToken` must be passed `TestContext.Current.CancellationToken`. Otherwise, the `xUnit1051` analyzer warns, and the build fails if `TreatWarningsAsErrors` is enabled.
- `InitializeAsync` / `DisposeAsync` of `IAsyncLifetime` return `ValueTask`.

## Related APIs

- @D3Fragments.Testing.ResultAssert
- @D3Fragments.Testing.DomainEventAssert
- @D3Fragments.Testing.TestData
- @D3Fragments.Testing.TestTime
- <xref:D3Fragments.Testing.EFCore.DatabaseContainerFixture`1>
- <xref:D3Fragments.Testing.EFCore.TransactionalTestBase`2>
