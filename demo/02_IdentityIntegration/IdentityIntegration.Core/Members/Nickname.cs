using System.Diagnostics.CodeAnalysis;

using D3Fragments;

namespace IdentityIntegration.Core.Members;

// The member's name shown on the pages (separate from the user name used to sign in)
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
            errorMessage = $"The nickname must be between 1 and {MaxLength} characters.";
            return false;
        }
        errorMessage = null;
        return true;
    }
}
