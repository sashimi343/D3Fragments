using System;
using System.Collections.Generic;

using D3Fragments;

namespace BasicWebApp.Domain.Books;

public class BookPlacedOnHoldDomainEvent : IDomainEvent
{
    public BookPlacedOnHoldDomainEvent(DateTimeOffset now, UuidV7 bookId, UuidV7 memberId)
    {
        OccurredAt = now;
        BookId = bookId;
        MemberId = memberId;
    }

    public DateTimeOffset OccurredAt { get; }
    public UuidV7 BookId { get; }
    public UuidV7 MemberId { get; }
}
