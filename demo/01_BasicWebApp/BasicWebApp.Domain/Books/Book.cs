using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Loans;
using BasicWebApp.Domain.Members;

using D3Fragments;

namespace BasicWebApp.Domain.Books;

public class Book : AggregateRoot
{
    private readonly List<Reservation> _reservations = new();

    private Book(Isbn isbn, BookTitle title, string author, BookStatus status)
    {
        Isbn = isbn;
        Title = title;
        Author = author;
        Status = status;
    }

    // For EF Core (Complex Types cannot be bound to constructor parameters)
    private Book()
    {
        Isbn = null!;
        Title = null!;
        Author = null!;
    }

    public Isbn Isbn { get; private set; }
    public BookTitle Title { get; private set; }
    public string Author { get; private set; }
    public BookStatus Status { get; private set; }
    public IReadOnlyCollection<Reservation> Reservations => _reservations;

    public static Book Create(string isbn, string title, string author)
    {
        return TryCreate(isbn, title, author).GetValueOrThrow();
    }

    public static Result<Book> TryCreate(string isbn, string title, string author)
    {
        Result<Isbn> isbnResult = Isbn.TryCreate(isbn);
        Result<BookTitle> titleResult = BookTitle.TryCreate(title);
        if (isbnResult.IsFailure || titleResult.IsFailure)
        {
            return Result<Book>.Failure(Result.CombineErrors(isbnResult, titleResult));
        }

        return Result<Book>.Success(new Book(isbnResult.Value, titleResult.Value, author, BookStatus.Available));
    }

    public Loan LendTo(Member member, DateOnly today)
    {
        if (Status == BookStatus.OnHold)
        {
            // The reservation is fulfilled when the member the book is held for borrows it (B7)
            Reservation hold = _reservations[0];
            if (hold.MemberId != member.Id)
            {
                throw new DomainException(Error.InvalidState("This book cannot be borrowed because it is reserved."));
            }
            _reservations.Remove(hold);
        }
        else if (Status != BookStatus.Available)
        {
            throw new DomainException(Error.InvalidState("This book cannot be borrowed."));
        }

        Status = BookStatus.OnLoan;
        return Loan.Create(member, this, today);
    }

    public void AcceptReturn(DateOnly returnedOn, DateTimeOffset now)
    {
        HoldForNextReservation(returnedOn, now);
    }

    public void AcceptReservation(Member member, DateOnly reservedOn, UuidV7? borrowerId)
    {
        if (!(Status == BookStatus.OnLoan || Status == BookStatus.OnHold))
        {
            throw new DomainException(Error.InvalidState("A book on the shelf cannot be reserved (it can be borrowed)."));
        }
        if (member.IsLoanSuspended(reservedOn))
        {
            throw new DomainException(Error.PreconditionFailed("You cannot reserve books while your loans are suspended."));
        }
        if (borrowerId == member.Id)
        {
            throw new DomainException(Error.PreconditionFailed("You cannot reserve a book you are borrowing."));
        }
        if (_reservations.Any(r => r.MemberId == member.Id))
        {
            throw new DomainException(Error.Conflict("You have already reserved this book."));
        }

        _reservations.Add(new Reservation(Id, member.Id, reservedOn));
    }

    public void CancelReservation(Member member, DateTimeOffset now)
    {
        Reservation? reservation = _reservations.FirstOrDefault(r => r.MemberId == member.Id);
        if (reservation is null)
        {
            throw new DomainException(Error.InvalidState("The reservation does not exist."));
        }

        // Move the hold to the next reservation only when the member the book is held for cancels (B8)
        bool isHoldTarget = Status == BookStatus.OnHold && reservation == _reservations[0];
        _reservations.Remove(reservation);
        if (isHoldTarget)
        {
            HoldForNextReservation(DateOnly.FromDateTime(now.DateTime), now);
        }
    }

    public void ExpireHold(DateTimeOffset now)
    {
        DateOnly today = DateOnly.FromDateTime(now.DateTime);
        Reservation? firstReservation = _reservations.FirstOrDefault();
        if (Status != BookStatus.OnHold || firstReservation is null || !firstReservation.IsHoldExpired(today))
        {
            return;
        }

        _reservations.Remove(firstReservation);
        HoldForNextReservation(today, now);
    }

    // ID of the member the book is held for (null if not on hold)
    public UuidV7? HoldMemberId => Status == BookStatus.OnHold ? _reservations[0].MemberId : null;

    public DeletionToken<Book> IssueDeletionToken()
    {
        if (Status != BookStatus.Available)
        {
            throw new DomainException(ErrorType.InvalidState, "Only books on the shelf can be withdrawn.");
        }
        if (Reservations.Any())
        {
            throw new DomainException(ErrorType.InvalidState, "A reserved book cannot be withdrawn.");
        }

        return base.IssueDeletionToken(this);
    }

    // Place the book on hold for the first reservation. If there are no reservations, return it to the shelf (B6, B8)
    private void HoldForNextReservation(DateOnly today, DateTimeOffset now)
    {
        Reservation? nextReservation = _reservations.FirstOrDefault();
        if (nextReservation is null)
        {
            Status = BookStatus.Available;
            return;
        }

        nextReservation.StartHold(today);
        Status = BookStatus.OnHold;
        AddDomainEvent(new BookPlacedOnHoldDomainEvent(now, Id, nextReservation.MemberId));
    }
}
