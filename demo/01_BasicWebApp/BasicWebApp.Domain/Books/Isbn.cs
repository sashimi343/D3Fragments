using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

using D3Fragments;

namespace BasicWebApp.Domain.Books;

public partial class Isbn : ValueObjectBase<Isbn, string>, IValueObject<Isbn, string>
{
    private static readonly string[] ValidPrefix = new string[] { "978", "979" };

    private Isbn([DisallowNull] string value) : base(value)
    {
    }

    public static Isbn Create(string value)
    {
        if (!ValidateValue(value, out var errorMessage))
        {
            throw new DomainValidationException(errorMessage);
        }
        // Normalize to the form without hyphens so that values with and without hyphens are equal
        return new Isbn(value.Replace("-", ""));
    }

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrEmpty(value))
        {
            errorMessage = "Specify an ISBN.";
            return false;
        }
        if (!IsValidFormat(value))
        {
            errorMessage = "The ISBN format is invalid.";
            return false;
        }
        if (!IsValidPrefix(value))
        {
            errorMessage = "The ISBN prefix is invalid.";
            return false;
        }
        if (!IsValidCheckDigit(value))
        {
            errorMessage = "The ISBN check digit is invalid.";
            return false;
        }

        errorMessage = null;
        return true;
    }

    private static bool IsValidFormat(string value)
    {
        return Isbn13Pattern().IsMatch(value) && value.Replace("-", "").Length == 13;
    }

    private static bool IsValidPrefix(string value)
    {
        var prefix = value.Substring(0, 3);
        return Array.Exists(ValidPrefix, p => p == prefix);
    }

    private static bool IsValidCheckDigit(string value)
    {
        var digits = ToDigitArray(value.Replace("-", ""));
        var checkDigit = digits[12];
        var sum = 0;
        for (int i = 0; i < 12; i++)
        {
            sum += digits[i] * (i % 2 == 0 ? 1 : 3);
        }
        var calculatedCheckDigit = (10 - (sum % 10)) % 10;
        return checkDigit == calculatedCheckDigit;
    }

    private static int[] ToDigitArray(string value)
    {
        return [.. value.ToCharArray().Select(c => c - '0')];
    }

    [GeneratedRegex(@"^(?:\d{3}-\d{1,5}-\d{1,7}-\d{1,7}-\d|\d{13})$")]
    private static partial Regex Isbn13Pattern();
}
