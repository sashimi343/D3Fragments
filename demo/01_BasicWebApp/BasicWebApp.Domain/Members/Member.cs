using System;
using System.Collections.Generic;

using D3Fragments;

namespace BasicWebApp.Domain.Members;

public class Member : AggregateRoot
{
    private Member(MemberName memberName, EmailAddress emailAddress, MemberType memberType, DateOnly? loanSuspendedUntil = null)
    {
        Name = memberName;
        EmailAddress = emailAddress;
        MemberType = memberType;
        LoanSuspendedUntil = loanSuspendedUntil;
    }

    // EF Core用（Complex Typeはコンストラクタ引数にバインドできない）
    private Member()
    {
        Name = null!;
        EmailAddress = null!;
        MemberType = null!;
    }

    public MemberName Name { get; private set; }
    public EmailAddress EmailAddress { get; private set; }
    public MemberType MemberType { get; private set; }
    public DateOnly? LoanSuspendedFrom { get; private set; }
    public DateOnly? LoanSuspendedUntil { get; private set; }

    public static Member Create(string memberName, string emailAddress, MemberType memberType)
    {
        return TryCreate(memberName, emailAddress, memberType).GetValueOrThrow();
    }

    public static Result<Member> TryCreate(string memberName, string emailAddress, MemberType memberType)
    {
        Result<MemberName> memberNameResult = MemberName.TryCreate(memberName);
        Result<EmailAddress> emailAddressResult = EmailAddress.TryCreate(emailAddress);
        if (memberNameResult.IsFailure || emailAddressResult.IsFailure)
        {
            return Result<Member>.Failure(Result.CombineErrors(memberNameResult, emailAddressResult));
        }

        return Result<Member>.Success(new Member(memberNameResult.Value, emailAddressResult.Value, memberType));
    }

    public bool IsLoanSuspended(DateOnly today)
    {
        return LoanSuspendedFrom <= today && LoanSuspendedUntil >= today;
    }

    public void SuspendLoan(DateOnly returnedOn, int suspensionDays)
    {
        // 返却日の翌日からsuspensionDays日間（停止期限は当日を含む）
        DateOnly newSuspendedFrom = returnedOn.AddDays(1);
        DateOnly newSuspendedUntil = returnedOn.AddDays(suspensionDays);
        if (!LoanSuspendedUntil.HasValue || LoanSuspendedUntil.Value < returnedOn)
        {
            LoanSuspendedFrom = newSuspendedFrom;
            LoanSuspendedUntil = newSuspendedUntil;
            return;
        }

        if (!LoanSuspendedFrom.HasValue || newSuspendedFrom < LoanSuspendedFrom.Value)
        {
            LoanSuspendedFrom = newSuspendedFrom;
        }
        if (!LoanSuspendedUntil.HasValue || newSuspendedUntil > LoanSuspendedUntil.Value)
        {
            LoanSuspendedUntil = newSuspendedUntil;
        }
    }
}
