using System;
using System.Collections.Generic;

using BasicWebApp.Domain.Books;
using BasicWebApp.Domain.Members;

using D3Fragments;

namespace BasicWebApp.Domain.Loans;

public readonly record struct LoanEligibilityRuleContext
{
    public Member Member { get; init; }
    public Book Book { get; init; }
    public IReadOnlyCollection<Loan> ActiveLoans { get; init; }
    public DateOnly Today { get; init; }
}

public class LoanEligibilityRule : IDomainRule<LoanEligibilityRuleContext>
{
    public Result Check(LoanEligibilityRuleContext context)
    {
        DateOnly today = context.Today;
        Member member = context.Member;
        Book book = context.Book;
        IReadOnlyCollection<Loan> activeLoans = context.ActiveLoans;

        if (member.IsLoanSuspended(today))
        {
            return Result.Failure(Error.PreconditionFailed("You cannot borrow books while your loans are suspended."));
        }
        if (activeLoans.Any(l => l.GetOverdueDays(today) > 0))
        {
            return Result.Failure(Error.PreconditionFailed("You cannot borrow books while you have overdue books."));
        }
        if (activeLoans.Count >= member.MemberType.LoanLimit)
        {
            return Result.Failure(Error.PreconditionFailed("You cannot borrow books because you have reached your loan limit."));
        }

        switch (book.Status)
        {
            case BookStatus.Available:
                return Result.Success();
            case BookStatus.OnHold:
                if (book.HoldMemberId == member.Id)
                {
                    return Result.Success();
                }
                else
                {
                    return Result.Failure(Error.InvalidState("This book cannot be borrowed because it is reserved."));
                }
            default:
                return Result.Failure(Error.InvalidState("This book cannot be borrowed."));
        }
    }
}
