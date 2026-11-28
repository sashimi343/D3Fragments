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

## Executing a command in a separate scope

One call to `IRequestExecutor` is one unit of saving.
If the handler throws an exception, all changes of that Command are discarded.
Execute processing like the following in a scope separate from the caller by using `IIsolatedRequestExecutor<TRequest, TResponse>`:

- A preprocessing step that must be committed in a separate transaction before the main processing
- A record that must remain even if the main processing fails (such as a record of an attempt to call an external API)

`IIsolatedRequestExecutor` creates a new dependency injection scope and executes the request with the `IRequestExecutor` of that scope.
Because the `DbContext` and `IUnitOfWork` are different instances from the caller's, the request is saved and committed independently of the caller.
The pipeline behaviors and the dispatch of domain events work the same as in a normal execution.
Because it inherits `IRequestExecutor`, `TryExecuteAsync` is also available.

```csharp
public sealed class ChangePlanCommandHandler(
    IIsolatedRequestExecutor<StartPaymentAttemptCommand, UuidV7> startAttempt,
    IPaymentAttemptRepository attempts,
    IPaymentGateway paymentGateway)
    : IRequestHandler<ChangePlanCommand, Result>
{
    public async Task<Result> HandleAsync(ChangePlanCommand request, CancellationToken cancellationToken)
    {
        // Commit the attempt record before calling the external API. It remains even if the main processing fails later
        UuidV7 attemptId = await startAttempt.ExecuteAsync(new StartPaymentAttemptCommand(request.SubscriptionId), cancellationToken);

        await paymentGateway.ChangePlanAsync(request.SubscriptionId, request.Plan, cancellationToken);

        // Complete the attempt in the same transaction as the main changes
        PaymentAttempt attempt = await attempts.FindAsync(attemptId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(PaymentAttempt));
        attempt.Complete();
        // The main changes
        return Result.Success();
    }
}
```

In this example, if saving or committing the main changes fails, the attempt record remains incomplete.
By detecting attempt records that remain incomplete, you can notice inconsistencies between the external API and the database.

When executing in a separate scope, note the following:

- Do not execute a Command with `IRequestExecutor` from a command handler or a before-commit domain event handler ([DFR0018](../../analyzers/DFR0018.md)). Because they share the `DbContext`, saving the inner Command also saves the changes that the caller has not saved yet. After-commit handlers can use `IRequestExecutor`, because the commit has already completed
- Do not execute a Command from a query handler ([DFR0019](../../analyzers/DFR0019.md))
- The inner Command cannot see rows that the caller has not committed yet. Inserting a row with a foreign key to such a row causes a constraint violation or waits for the lock to be released indefinitely
- If the inner Command updates a row locked by the caller's transaction, the inner Command waits indefinitely (the caller waits for the inner Command to complete, so neither proceeds). The library cannot detect this
- The state held by scoped services (such as caches) is not carried over to the new scope. `IHttpContextAccessor` returns the same `HttpContext` in the new scope
- One request uses two database connections. Be careful about the upper limit of the connection pool
- In unit tests of a handler, replace `IIsolatedRequestExecutor` with a spy and verify that it was called and the content of the request. This is because the request runs on a connection other than the `DbContext` of `TransactionalTestBase` and is not rolled back by the test

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
- <xref:D3Fragments.Application.IIsolatedRequestExecutor`2>
