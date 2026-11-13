# Service registration

The features of D3Fragments are registered in the DI container with an extension method for each package.
This page summarizes the services each method registers and the rules that apply to registration.

## Registration example

The following is the registration-related part of `Program.cs` in the [demo application](https://github.com/sashimi343/D3Fragments/tree/main/demo/01_BasicWebApp).

```csharp
var builder = WebApplication.CreateBuilder(args);

// Application layer: handlers, pipeline behaviors, domain event handlers
builder.Services.AddD3FragmentsApplication([typeof(GetBookQuery).Assembly]);

// Infrastructure layer: DbContext, Unit of Work, repositories
builder.Services.AddD3FragmentsEFCore<BasicWebAppDbContext>(
    [typeof(MemberType).Assembly, typeof(BookRepository).Assembly],
    options => options.UseSqlite(connectionString));

// Presentation layer: error responses, JSON converter for value objects
builder.Services.AddD3FragmentsAspNetCore();

var app = builder.Build();

app.UseExceptionHandler("/Error");
// ...
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api => api.UseCsrfProtection());
```

You need to call both `AddD3FragmentsApplication` and `AddD3FragmentsEFCore`.
The pipeline behaviors registered by `AddD3FragmentsApplication` use `IUnitOfWork`, and the `IUnitOfWork` implementation is registered by `AddD3FragmentsEFCore`.

## Common rules

- No method replaces services that are already registered. To replace a default implementation with your own, register yours before calling the method (see [Replacing default implementations](#replacing-default-implementations)).
- Calling the same method more than once does not register services twice.
- Methods that take assemblies search them for types, excluding abstract types and generic type definitions, and register those types. They throw `ArgumentException` if no assembly is passed.
- `ILogger<T>` is not registered. The generic host and ASP.NET Core register it by default.

## AddD3FragmentsApplication

A method in the `Sashimi343.D3Fragments.Application` package.

```csharp
services.AddD3FragmentsApplication(assemblies, configure: null);
```

| Parameter | Description |
|---|---|
| `assemblies` | Assemblies to search for handlers and domain event handlers. At least one is required |
| `configure` | A delegate that configures `D3FragmentsApplicationOptions` (optional). See [Domain event handlers](04-application/03-domain-event-handlers.md) for the settings |

It registers the following services.

| Service | Implementation | Lifetime |
|---|---|---|
| `IRequestExecutor<TRequest, TResponse>` | `RequestExecutor<TRequest, TResponse>` | Scoped |
| `IPipelineBehavior<TRequest, TResponse>` | `TransactionBehavior<TRequest, TResponse>`, `UnitOfWorkBehavior<TRequest, TResponse>` | Scoped |
| `IDomainEventDispatcher` | `DomainEventDispatcher` | Scoped |
| `IRequestHandler<TRequest, TResponse>` | Implementations in `assemblies` | Scoped |
| `IBeforeCommitDomainEventHandler<TEvent>`, `IAfterCommitDomainEventHandler<TEvent>` | Implementations in `assemblies` (multiple handlers can be registered for the same event) | Scoped |
| `TimeProvider` | `TimeProvider.System` | Singleton |

If the request type of a handler implements neither `ICommand` nor `IQuery`, or implements both, the method throws `InvalidOperationException` (see [Commands and Queries](04-application/01-commands-and-queries.md)).

## AddD3FragmentsEFCore

A method in the `Sashimi343.D3Fragments.EFCore` package.

```csharp
services.AddD3FragmentsEFCore<TContext>(assemblies, configureDbContext);
```

| Parameter | Description |
|---|---|
| `TContext` | The application's `DbContext`. It must derive from `D3DbContext` |
| `assemblies` | Assemblies to search for type-safe enums and repository implementations. At least one is required |
| `configureDbContext` | A delegate that configures the `DbContext`, for example by specifying the database provider (`UseNpgsql` and so on) |

It registers the following services.

| Service | Implementation | Lifetime |
|---|---|---|
| `TContext` | The `DbContext`, with `UseD3Fragments(assemblies)` applied after `configureDbContext` (registered with `AddDbContext`) | Scoped |
| `IUnitOfWork` | `EfUnitOfWork<TContext>` | Scoped |
| `IDomainEventDispatchStrategy` | `ImmediateDispatchStrategy` | Scoped |
| Repository interfaces | Implementations in `assemblies` | Scoped |

`IDomainEventDispatchStrategy` is how domain events are delivered to after-commit handlers.
The default `ImmediateDispatchStrategy` calls the after-commit handlers right after the commit.

A repository implementation is registered under the `IRepository<TAggregate, TId>` it implements and under every interface derived from it.
For example, `BookRepository : RepositoryBase<…>, IBookRepository` can be resolved as both `IBookRepository` and `IRepository<Book, UuidV7>`.
If more than one implementation of the same interface is found, the method throws `InvalidOperationException`.

Value converters that persist type-safe enums by their key are registered for the type-safe enums in `assemblies`.
Therefore, also pass the domain layer assembly that defines the type-safe enums.

## AddD3FragmentsAspNetCore

A method in the `Sashimi343.D3Fragments.AspNetCore` package.

```csharp
services.AddD3FragmentsAspNetCore(configure: null);
```

| Parameter | Description |
|---|---|
| `configure` | A delegate that configures `DomainProblemDetailsOptions` (optional). See [Error responses](06-aspnetcore/01-error-responses.md) for the settings |

It registers the following:

- The Problem Details services (`AddProblemDetails`)
- An exception handler (`IExceptionHandler`) that converts `DomainException` into Problem Details responses
- The JSON converter for value objects (`ValueObjectJsonConverterFactory`), added to both the Minimal API `JsonOptions` and the MVC `JsonOptions`

To enable the exception handler, call `app.UseExceptionHandler()`.
The JSON converter for type-safe enums is not registered (see [Type-safe enums](03-domain/03-type-safe-enum.md)).

## Middleware and OpenAPI

In addition to DI registration, configure the following as needed.

| Setting | How | Details |
|---|---|---|
| CSRF protection middleware | `app.UseCsrfProtection()`. Configure it with `services.Configure<CsrfProtectionOptions>(...)` | [CSRF protection](06-aspnetcore/03-csrf.md) |
| OpenAPI document transformation | `services.AddOpenApi(options => options.AddD3Fragments())` | [OpenAPI](06-aspnetcore/04-openapi.md) |
| Custom pipeline behaviors | `services.AddScoped(typeof(IPipelineBehavior<,>), typeof(MyBehavior<,>))` | [Pipeline and transactions](04-application/02-pipeline-and-transactions.md) |

## Replacing default implementations

Because no method replaces services that are already registered, the implementation registered first is used.
The following example uses a `TimeProvider` whose current time can be changed, only in the development environment.

```csharp
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<TimeProvider>(new DemoTimeProvider());
}

// TimeProvider is already registered, so TimeProvider.System is not registered
builder.Services.AddD3FragmentsApplication([typeof(GetBookQuery).Assembly]);
```

In the same way, you can replace `IDomainEventDispatchStrategy` and other services with your own implementations.

## Related APIs

- @D3Fragments.Application.D3FragmentsApplicationServiceCollectionExtensions
- @D3Fragments.EFCore.D3FragmentsEFCoreServiceCollectionExtensions
- @D3Fragments.AspNetCore.D3FragmentsServiceCollectionExtensions
- @D3Fragments.AspNetCore.CsrfProtectionApplicationBuilderExtensions
- @D3Fragments.AspNetCore.OpenApi.D3FragmentsOpenApiOptionsExtensions
