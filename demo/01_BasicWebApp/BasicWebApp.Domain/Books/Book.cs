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

    // EF Core用（Complex Typeはコンストラクタ引数にバインドできない）
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
            // 取り置き対象の会員が借りた時点で、その予約は消化される（B7）
            Reservation hold = _reservations[0];
            if (hold.MemberId != member.Id)
            {
                throw new DomainException(Error.InvalidState("予約されている資料のため貸出できません。"));
            }
            _reservations.Remove(hold);
        }
        else if (Status != BookStatus.Available)
        {
            throw new DomainException(Error.InvalidState("この資料は貸出できません。"));
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
            throw new DomainException(Error.InvalidState("在架の資料は予約できません（貸出可能です）"));
        }
        if (member.IsLoanSuspended(reservedOn))
        {
            throw new DomainException(Error.PreconditionFailed("貸出停止中のため予約できません。"));
        }
        if (borrowerId == member.Id)
        {
            throw new DomainException(Error.PreconditionFailed("借りている資料は予約できません。"));
        }
        if (_reservations.Any(r => r.MemberId == member.Id))
        {
            throw new DomainException(Error.Conflict("既に予約済みです"));
        }

        _reservations.Add(new Reservation(Id, member.Id, reservedOn));
    }

    public void CancelReservation(Member member, DateTimeOffset now)
    {
        Reservation? reservation = _reservations.FirstOrDefault(r => r.MemberId == member.Id);
        if (reservation is null)
        {
            throw new DomainException(Error.InvalidState("予約が存在しません"));
        }

        // 取り置き対象の会員が取り消した場合のみ、次の予約者へ繰り上げる（B8）
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

    // 取り置き対象の会員ID（取り置き中でなければnull）
    public UuidV7? HoldMemberId => Status == BookStatus.OnHold ? _reservations[0].MemberId : null;

    public DeletionToken<Book> IssueDeletionToken()
    {
        if (Status != BookStatus.Available)
        {
            throw new DomainException(ErrorType.InvalidState, "在架でない資料は除籍できません。");
        }
        if (Reservations.Any())
        {
            throw new DomainException(ErrorType.InvalidState, "予約がある資料は除籍できません。");
        }

        return base.IssueDeletionToken(this);
    }

    // 先頭の予約者向けに取り置きする。予約がなければ在架に戻す（B6, B8）
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
