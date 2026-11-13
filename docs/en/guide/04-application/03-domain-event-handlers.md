# Domain event handlers

A domain event handler receives a [domain event](../03-domain/06-domain-events.md) raised by an aggregate and performs follow-up processing.
There are two kinds of handlers, which are called at different times.

| Interface | When it is called | Use |
|---|---|---|
| `IBeforeCommitDomainEventHandler<TEvent>` | Before the changes are saved (in the same transaction) | Processing that updates the database, such as updating other aggregates |
| `IAfterCommitDomainEventHandler<TEvent>` | After a successful commit | I/O outside the database, such as sending email or calling external APIs |

Handlers are registered automatically from the assemblies passed to `AddD3FragmentsApplication`.

## Before-commit handlers

Changes made in the handler are saved together with the business data.
If either the handler or the save fails, everything is rolled back together.
Exceptions thrown by the handler propagate to the caller of the Command.

```csharp
public sealed class SuspendMemberOnOverdueReturnHandler(IMemberRepository members)
    : IBeforeCommitDomainEventHandler<BookReturnedDomainEvent>
{
    public async Task HandleAsync(BookReturnedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent.ReturnedOn <= domainEvent.DueDate)
        {
            return;
        }

        Member member = await members.FindAsync(domainEvent.MemberId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Member));
        int overdueDays = domainEvent.ReturnedOn.DayNumber - domainEvent.DueDate.DayNumber;
        member.SuspendLoan(domainEvent.ReturnedOn, overdueDays);
    }
}
```

An aggregate changed by a handler may raise new events.
In that case, the before-commit handlers for the new events are called as well.
To prevent handlers from raising events for each other indefinitely, this is repeated at most 10 times.
If that limit is exceeded, `InvalidOperationException` is thrown.

## After-commit handlers

Because the business data has already been committed, exceptions thrown by the handler do not propagate to the caller of the Command.
The exception is written to the error log, and the remaining handlers are still called.

```csharp
public sealed class NotifyBookPlacedOnHoldHandler(ILogger<NotifyBookPlacedOnHoldHandler> logger)
    : IAfterCommitDomainEventHandler<BookPlacedOnHoldDomainEvent>
{
    public Task HandleAsync(BookPlacedOnHoldDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Notify the member that the book is on hold
        logger.LogInformation("BookId '{BookId}' has been placed on hold.", domainEvent.BookId);
        return Task.CompletedTask;
    }
}
```

Do not change entities directly in an after-commit handler.
Such changes are not saved, and the fact that they were discarded is written to the error log.
If you need to update the database, execute another Command from the handler.

The default dispatcher passes `CancellationToken.None` as `cancellationToken`.
This prevents the caller's cancellation from interrupting the follow-up processing of committed changes.

## When handlers are called

- Only handlers whose type argument `TEvent` matches the runtime type of the event are called. Handlers whose `TEvent` is a base type or an interface of the event are not called.
- If there are multiple handlers for the same event, they are called one at a time in registration order.

## Log event IDs

You can change the event IDs of the error logs written after the commit with `D3FragmentsApplicationOptions`.

| Property | Default | Logged when |
|---|---|---|
| `AfterCommitHandlerFailedEventId` | 1001 | An after-commit handler threw an exception |
| `AfterCommitChangesDiscardedEventId` | 1002 | Changes made by an after-commit handler were discarded |

```csharp
builder.Services.AddD3FragmentsApplication(
    [typeof(RegisterBookCommand).Assembly],
    options => options.AfterCommitHandlerFailedEventId = new EventId(5001, "AfterCommitHandlerFailed"));
```

## Related APIs

- <xref:D3Fragments.Application.IBeforeCommitDomainEventHandler`1>
- <xref:D3Fragments.Application.IAfterCommitDomainEventHandler`1>
- @D3Fragments.Application.D3FragmentsApplicationOptions
