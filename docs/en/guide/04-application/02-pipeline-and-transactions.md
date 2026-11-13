# Pipeline and transactions

`IRequestExecutor` executes a handler wrapped in pipeline behaviors.
Pipeline behaviors take care of processing common to all requests, such as saving changes and managing transactions.

## Built-in pipeline behaviors

`AddD3FragmentsApplication` registers the following two pipeline behaviors.

| Behavior | `Order` | Processing |
|---|---:|---|
| `TransactionBehavior` | 200 | For Commands that implement `ITransactionalCommand`, wraps the entire handler execution in an explicit database transaction |
| `UnitOfWorkBehavior` | 300 | For requests other than Queries, saves the changes when the handler completes successfully. Discards the changes if an exception is thrown |

Behaviors with a smaller `Order` run further out.
Therefore, for a Command that uses `TransactionBehavior`, the save performed by `UnitOfWorkBehavior` is also included in the transaction.

## Saving changes

`UnitOfWorkBehavior` calls `IUnitOfWork.SaveChangesAsync` once after the handler completes successfully.
If the handler throws an exception or the operation is canceled, it discards the changes without saving and rethrows the exception as is.

This single save is enough for most Commands,
because `SaveChangesAsync` on its own runs as a single transaction.

## Explicit transactions

If you need to start a transaction before the handler runs (for example, to take row locks), add `ITransactionalCommand` to the Command.

```csharp
// Reserve a seat after locking its row
public sealed record ReserveSeatCommand(UuidV7 SeatId, UuidV7 MemberId)
    : ICommand<UuidV7>, ITransactionalCommand;
```

`TransactionBehavior` runs the rest of the pipeline inside `IUnitOfWork.ExecuteInTransactionAsync`.
If no exception is thrown, it commits; if one is thrown, it rolls back.

## Adding your own pipeline behavior

Add common processing such as logging by implementing `IPipelineBehavior<TRequest, TResponse>`.

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public int Order => 100;

    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing {Request}", typeof(TRequest).Name);
        var response = await next(cancellationToken);
        logger.LogInformation("Executed {Request}", typeof(TRequest).Name);
        return response;
    }
}
```

```csharp
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

The execution order is determined by the `Order` value, not by the DI registration order.
In the example above, `Order` is 100, so the behavior runs outside the built-in behaviors.

Calling `next` runs the next step of the pipeline (the next behavior or the handler).

## Related APIs

- <xref:D3Fragments.Application.IPipelineBehavior`2>
- <xref:D3Fragments.Application.UnitOfWorkBehavior`2>
- <xref:D3Fragments.Application.TransactionBehavior`2>
- @D3Fragments.Application.ITransactionalCommand
- @D3Fragments.Application.IUnitOfWork
