# Domain events

A domain event is an object that represents something that happened in the domain (such as "a book was placed on hold").
It is raised as a result of an operation on an aggregate and leads to follow-up processing, such as updating other aggregates or sending notifications.

## Defining an event

Implement `IDomainEvent`.
Set `OccurredAt` to the time when the event happened.

```csharp
using D3Fragments;

public sealed record BookPlacedOnHoldDomainEvent(DateTimeOffset OccurredAt, UuidV7 BookId, UuidV7 MemberId)
    : IDomainEvent;
```

## Raising an event

Only aggregate roots can raise domain events.
Call `AddDomainEvent` in a method of the aggregate root.

```csharp
public sealed class Book : AggregateRoot
{
    private void HoldForNextReservation(DateOnly today, DateTimeOffset now)
    {
        // ...
        Status = BookStatus.OnHold;
        AddDomainEvent(new BookPlacedOnHoldDomainEvent(now, Id, nextReservation.MemberId));
    }
}
```

`AddDomainEvent` only records the event in the aggregate.
No handler is called at this point.

## When events are processed

Recorded events are processed when the Unit of Work saves the changes at the completion of the Command.

1. Before saving the changes, the before-commit handlers (`IBeforeCommitDomainEventHandler<TEvent>`) are called. Changes made by the handlers are saved in the same transaction as the business data
2. The changes are saved and committed
3. After the commit, the after-commit handlers (`IAfterCommitDomainEventHandler<TEvent>`) are called
4. The events recorded in the aggregates are cleared

If the handler throws an exception and the changes are discarded, the events are discarded too, and no handler is called.

See [Domain event handlers](../04-application/03-domain-event-handlers.md) for how to implement handlers.

## Related APIs

- @D3Fragments.IDomainEvent
- @D3Fragments.IHasDomainEvents
- <xref:D3Fragments.AggregateRoot`1>
