using System;
using System.Collections.Generic;

using D3Fragments;

namespace BasicWebApp.Domain.Loans;

public class BookReturnedDomainEvent : IDomainEvent
{
    public BookReturnedDomainEvent(DateTimeOffset now, UuidV7 bookId, UuidV7 memberId, DateOnly dueDate, DateOnly returnedOn)
    {
        OccurredAt = now;
        BookId = bookId;
        MemberId = memberId;
        DueDate = dueDate;
        ReturnedOn = returnedOn;
    }

    public DateTimeOffset OccurredAt { get; }
    public UuidV7 BookId { get; }
    public UuidV7 MemberId { get; }
    public DateOnly DueDate { get; }
    public DateOnly ReturnedOn { get; }
}
