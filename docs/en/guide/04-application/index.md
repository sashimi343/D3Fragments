# Application layer

The application layer contains the use cases (Commands and Queries) and their handlers.
D3Fragments provides the infrastructure for executing use cases in the `Sashimi343.D3Fragments.Application` package.

| Page | Main types |
|---|---|
| [Commands and Queries](01-commands-and-queries.md) | `ICommand<TResponse>`, `IQuery<TResponse>`, `IRequestHandler<TRequest, TResponse>`, `IRequestExecutor<TRequest, TResponse>` |
| [Pipeline and transactions](02-pipeline-and-transactions.md) | `IPipelineBehavior<TRequest, TResponse>`, `UnitOfWorkBehavior`, `TransactionBehavior`, `ITransactionalCommand` |
| [Domain event handlers](03-domain-event-handlers.md) | `IBeforeCommitDomainEventHandler<TEvent>`, `IAfterCommitDomainEventHandler<TEvent>` |

## Service registration

Pass the assemblies that define your handlers to `AddD3FragmentsApplication`.

```csharp
builder.Services.AddD3FragmentsApplication([typeof(RegisterBookCommand).Assembly]);
```

The handlers and domain event handlers in the assemblies you pass are registered automatically.
This method does not register an `IUnitOfWork` implementation, so also call `AddD3FragmentsEFCore` from the EF Core integration.
See [Service registration](../02-service-registration.md) for the list of registered services.
