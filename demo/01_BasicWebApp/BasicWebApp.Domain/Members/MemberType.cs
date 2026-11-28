using System;
using System.Collections.Generic;

using D3Fragments;

namespace BasicWebApp.Domain.Members;

public class MemberType : TypeSafeEnum<MemberType, int>
{
    public static readonly MemberType Regular = new(0, "Regular", 5, 14);
    public static readonly MemberType Student = new(1, "Student", 10, 28);

    private MemberType(int key, string name, int loanLimit, int loanPeriodDays) : base(key, name)
    {
        LoanLimit = loanLimit;
        LoanPeriodDays = loanPeriodDays;
    }

    public int LoanLimit { get; }
    public int LoanPeriodDays { get; }
}
