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

    // 検証はValueObjectBaseのコンストラクタで行われる
    public static EmailAddress Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "メールアドレスを指定してください。";
            return false;
        }
        if (!System.Net.Mail.MailAddress.TryCreate(value, out var _))
        {
            errorMessage = "メールアドレスの形式が不正です。";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
