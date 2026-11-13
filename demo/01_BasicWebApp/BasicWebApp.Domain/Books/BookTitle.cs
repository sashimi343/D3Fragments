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

    // 検証はValueObjectBaseのコンストラクタで行われる
    public static BookTitle Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "書名を指定してください。";
            return false;
        }
        if (value.Trim().Length > 200)
        {
            errorMessage = "書名は1文字以上、200文字以内で指定してください。";
            return false;
        }
        errorMessage = null;
        return true;
    }
}
