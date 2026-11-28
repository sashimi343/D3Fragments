using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using D3Fragments;

namespace BasicWebApp.Domain.Members;

public class EmailAddress : ValueObjectBase<EmailAddress, string>, IValueObject<EmailAddress, string>
{
    private EmailAddress(string value) : base(value)
    {
    }

    // Validation is performed by the ValueObjectBase constructor
    public static EmailAddress Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Specify an email address.";
            return false;
        }
        if (!System.Net.Mail.MailAddress.TryCreate(value, out var _))
        {
            errorMessage = "The email address format is invalid.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
