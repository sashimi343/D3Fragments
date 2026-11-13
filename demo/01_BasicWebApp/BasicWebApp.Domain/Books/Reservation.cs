using System;
using System.Collections.Generic;

using D3Fragments;

namespace BasicWebApp.Domain.Books;

public class Reservation : Entity
{
    private const int HoldDays = 7;

    internal Reservation(UuidV7 bookId, UuidV7 memberId, DateOnly reservedOn)
    {
        BookId = bookId;
        MemberId = memberId;
        ReservedOn = reservedOn;
    }

    public UuidV7 BookId { get; private set; }
    public UuidV7 MemberId { get; private set; }
    public DateOnly ReservedOn { get; private set; }

    // 取り置き期限（取り置き中の予約のみ値を持つ）
    public DateOnly? HoldUntil { get; private set; }

    internal void StartHold(DateOnly today)
    {
        HoldUntil = today.AddDays(HoldDays);
    }

    internal bool IsHoldExpired(DateOnly today)
    {
        return HoldUntil < today;
    }
}
