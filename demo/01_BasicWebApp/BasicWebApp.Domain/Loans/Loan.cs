using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Members;

using D3Fragments;

namespace BasicWebApp.Domain.Loans;

public class Loan : AggregateRoot
{
    private Loan(UuidV7 bookId, UuidV7 memberId, DateOnly loanedOn, DateOnly dueDate, int renewalCount, DateOnly? returnedOn)
    {
        BookId = bookId;
        MemberId = memberId;
        LoanedOn = loanedOn;
        DueDate = dueDate;
        RenewalCount = renewalCount;
        ReturnedOn = returnedOn;
    }

    public UuidV7 BookId { get; private set; }
    public UuidV7 MemberId { get; private set; }
    public DateOnly LoanedOn { get; private set; }
    public DateOnly DueDate { get; private set; }
    public int RenewalCount { get; private set; }
    public DateOnly? ReturnedOn { get; private set; }

    public static Loan Create(Member member, Book book, DateOnly loanedOn)
    {
        DateOnly dueDate = loanedOn.AddDays(member.MemberType.LoanPeriodDays);
        return new Loan(book.Id, member.Id, loanedOn, dueDate, 0, null);
    }

    public void Renew(DateOnly today, int loanPeriodDays, bool hasReservations)
    {
        if (IsReturned())
        {
            throw new DomainException(Error.InvalidState("既に返却済みのため延長できません。"));
        }
        if (RenewalCount >= 1)
        {
            throw new DomainException(Error.PreconditionFailed("延長は1回までしかできません。"));
        }
        if (DueDate < today)
        {
            throw new DomainException(Error.PreconditionFailed("返却期限を過ぎているため延長できません。"));
        }
        if (hasReservations)
        {
            throw new DomainException(Error.PreconditionFailed("他の会員に予約されているため延長できません。"));
        }

        DateOnly newDueDate = today.AddDays(loanPeriodDays);
        DueDate = newDueDate;
        RenewalCount++;
    }

    public void Return(DateTimeOffset now)
    {
        if (IsReturned())
        {
            throw new DomainException(Error.InvalidState("既に返却済みのため返却できません。"));
        }

        ReturnedOn = DateOnly.FromDateTime(now.DateTime);
        AddDomainEvent(new BookReturnedDomainEvent(now, BookId, MemberId, DueDate, ReturnedOn.Value));
    }

    public int GetOverdueDays(DateOnly today)
    {
        // 返却済みなら返却日で確定する（L4）
        int days = (ReturnedOn ?? today).DayNumber - DueDate.DayNumber;
        return Math.Max(0, days);
    }

    public bool IsReturned()
    {
        return ReturnedOn.HasValue;
    }
}
