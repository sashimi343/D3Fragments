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
            return Result.Failure(Error.PreconditionFailed("貸出停止中のため貸出できません。"));
        }
        if (activeLoans.Any(l => l.GetOverdueDays(today) > 0))
        {
            return Result.Failure(Error.PreconditionFailed("延滞中の資料があるため貸出できません。"));
        }
        if (activeLoans.Count >= member.MemberType.LoanLimit)
        {
            return Result.Failure(Error.PreconditionFailed("貸出冊数の上限に達しているため貸出できません。"));
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
                    return Result.Failure(Error.InvalidState("予約されている資料のため貸出できません。"));
                }
            default:
                return Result.Failure(Error.InvalidState("この資料は貸出できません。"));
        }
    }
}
