using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using D3Fragments;

namespace BasicWebApp.Domain.Books;

public class BookTitle : ValueObjectBase<BookTitle, string>, IValueObject<BookTitle, string>
{
    private BookTitle([DisallowNull] string value) : base(value)
    {
    }

    // Validation is performed by the ValueObjectBase constructor
    public static BookTitle Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Specify a title.";
            return false;
        }
        if (value.Trim().Length > 200)
        {
            errorMessage = "The title must be between 1 and 200 characters.";
            return false;
        }
        errorMessage = null;
        return true;
    }
}
