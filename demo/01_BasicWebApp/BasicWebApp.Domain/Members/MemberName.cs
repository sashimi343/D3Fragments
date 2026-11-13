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

    // 検証はValueObjectBaseのコンストラクタで行われる
    public static MemberName Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "会員名を指定してください。";
            return false;
        }
        if (value.Trim().Length > 50)
        {
            errorMessage = "会員名は1文字以上、50文字以内で指定してください。";
            return false;
        }
        errorMessage = null;
        return true;
    }
}
