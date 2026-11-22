using System.Diagnostics.CodeAnalysis;

using D3Fragments;

namespace IdentityIntegration.Core.Members;

// 画面に表示する会員の名前（ログインに使うユーザー名とは別）
public sealed class Nickname : ValueObjectBase<Nickname, string>, IValueObject<Nickname, string>
{
    private const int MaxLength = 20;

    private Nickname(string value) : base(value)
    {
    }

    public static Nickname Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > MaxLength)
        {
            errorMessage = $"ニックネームは1文字以上、{MaxLength}文字以内で指定してください。";
            return false;
        }
        errorMessage = null;
        return true;
    }
}
