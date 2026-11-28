using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using D3Fragments;

namespace BasicWebApp.Domain.Members;

public class MemberName : ValueObjectBase<MemberName, string>, IValueObject<MemberName, string>
{
    private MemberName(string value) : base(value)
    {
    }

    // Validation is performed by the ValueObjectBase constructor
    public static MemberName Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Specify a member name.";
            return false;
        }
        if (value.Trim().Length > 50)
        {
            errorMessage = "The member name must be between 1 and 50 characters.";
            return false;
        }
        errorMessage = null;
        return true;
    }
}
