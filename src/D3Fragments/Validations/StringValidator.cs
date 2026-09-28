using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace D3Fragments.Validations;

/// <summary>
///     <para xml:lang="en">Defines and applies validation rules to a string. <see langword="null"/> is treated as a missing value; when <see cref="Required"/> is declared, an empty or whitespace-only string is treated as missing as well.</para>
///     <para xml:lang="ja">文字列に対するバリデーションルールを定義・適用するクラス。<see langword="null"/>を値が存在しないものとして扱う。<see cref="Required"/>を宣言した場合は空文字・空白のみの文字列も値が存在しないものとして扱う。</para>
/// </summary>
/// <example>
/// <code>
/// private static readonly StringValidator NameRule = new StringValidator("Name", target: "name").Required().MaxLength(50);
/// Result result = NameRule.Check(name);
/// </code>
/// </example>
public sealed partial class StringValidator : ValidatorBase<string>
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private readonly StringLengthUnit _lengthUnit;
    private bool _blankIsMissing;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="StringValidator"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="StringValidator"/> を初期化する。</para>
    /// </summary>
    /// <param name="displayName">
    ///     <para xml:lang="en">The display name of the value, used in error messages.</para>
    ///     <para xml:lang="ja">エラーメッセージに使用する値の表示名。</para>
    /// </param>
    /// <param name="target">
    ///     <para xml:lang="en">An optional key set to <see cref="Error.Target"/> of the resulting errors.</para>
    ///     <para xml:lang="ja">生成するエラーの<see cref="Error.Target"/>に設定するキー（省略可）。</para>
    /// </param>
    /// <param name="lengthUnit">
    ///     <para xml:lang="en">How to count the length for <see cref="MinLength"/>, <see cref="MaxLength"/> and <see cref="Length"/>.</para>
    ///     <para xml:lang="ja"><see cref="MinLength"/>・<see cref="MaxLength"/>・<see cref="Length"/>で文字数を数える単位。</para>
    /// </param>
    public StringValidator(string displayName, string? target = null, StringLengthUnit lengthUnit = StringLengthUnit.Utf16CodeUnit)
        : base(displayName, target)
    {
        if (!Enum.IsDefined(lengthUnit))
        {
            throw new ArgumentOutOfRangeException(nameof(lengthUnit));
        }
        _lengthUnit = lengthUnit;
    }

    /// <summary>
    ///     <para xml:lang="en">Validates the value.</para>
    ///     <para xml:lang="ja">値を検証する。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The value to validate.</para>
    ///     <para xml:lang="ja">検証対象の値。</para>
    /// </param>
    public Result Check(string? value) => Check(value, DisplayName);

    /// <summary>
    ///     <para xml:lang="en">Validates the value, overriding the display name used in error messages (e.g. with a localized name).</para>
    ///     <para xml:lang="ja">エラーメッセージに使用する表示名を上書き（多言語化した項目名等）して値を検証する。</para>
    /// </summary>
    /// <param name="value">
    ///     <para xml:lang="en">The value to validate.</para>
    ///     <para xml:lang="ja">検証対象の値。</para>
    /// </param>
    /// <param name="displayName">
    ///     <para xml:lang="en">The display name used in error messages.</para>
    ///     <para xml:lang="ja">エラーメッセージに使用する表示名。</para>
    /// </param>
    public Result Check(string? value, string displayName) =>
        Validate(value is not null && !(_blankIsMissing && string.IsNullOrWhiteSpace(value)), value ?? string.Empty, displayName);

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be non-null. An empty string is allowed; use <see cref="Required"/> to reject it.</para>
    ///     <para xml:lang="ja">値がnullでないことを検証する。空文字は許容する。空文字も禁止する場合は<see cref="Required"/>を使用する。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator NotNull(string? message = null)
    {
        _blankIsMissing = false;
        SetRequired(message, () => ValidationMessages.NotNull);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be neither null, empty nor whitespace-only.</para>
    ///     <para xml:lang="ja">値がnull・空文字・空白のみでないことを検証する。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator Required(string? message = null)
    {
        _blankIsMissing = true;
        SetRequired(message, () => ValidationMessages.Required);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the length to be at least <paramref name="min"/>.</para>
    ///     <para xml:lang="ja">文字数が<paramref name="min"/>以上であることを検証する。</para>
    /// </summary>
    /// <param name="min">
    ///     <para xml:lang="en">The minimum length (inclusive).</para>
    ///     <para xml:lang="ja">最小文字数（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="min"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="min"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator MinLength(int min, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(min);
        AddRule(value => CountLength(value) >= min, message, () => ValidationMessages.MinLength, min);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the length to be at most <paramref name="max"/>.</para>
    ///     <para xml:lang="ja">文字数が<paramref name="max"/>以下であることを検証する。</para>
    /// </summary>
    /// <param name="max">
    ///     <para xml:lang="en">The maximum length (inclusive).</para>
    ///     <para xml:lang="ja">最大文字数（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="max"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="max"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator MaxLength(int max, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(max);
        AddRule(value => CountLength(value) <= max, message, () => ValidationMessages.MaxLength, max);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the length to be exactly <paramref name="exact"/>.</para>
    ///     <para xml:lang="ja">文字数が<paramref name="exact"/>と一致することを検証する。</para>
    /// </summary>
    /// <param name="exact">
    ///     <para xml:lang="en">The required length.</para>
    ///     <para xml:lang="ja">文字数。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="exact"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="exact"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator Length(int exact, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(exact);
        AddRule(value => CountLength(value) == exact, message, () => ValidationMessages.Length, exact);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to match the regular expression. The pattern is compiled once with a 1-second match timeout; a timed-out match is treated as invalid.</para>
    ///     <para xml:lang="ja">値が正規表現に一致することを検証する。パターンはタイムアウト1秒で一度だけコンパイルされる。タイムアウトした場合は不一致として扱う。</para>
    /// </summary>
    /// <param name="pattern">
    ///     <para xml:lang="en">The regular expression pattern.</para>
    ///     <para xml:lang="ja">正規表現パターン。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator Matches(string pattern, string? message = null) =>
        Matches(new Regex(pattern, RegexOptions.None, RegexTimeout), message);

    /// <summary>
    ///     <para xml:lang="en">Requires the value to match the regular expression (e.g. one produced by <see cref="GeneratedRegexAttribute"/>). A timed-out match is treated as invalid.</para>
    ///     <para xml:lang="ja">値が正規表現（<see cref="GeneratedRegexAttribute"/>で生成したもの等）に一致することを検証する。タイムアウトした場合は不一致として扱う。</para>
    /// </summary>
    /// <param name="regex">
    ///     <para xml:lang="en">The regular expression.</para>
    ///     <para xml:lang="ja">正規表現。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator Matches(Regex regex, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(regex);
        AddRule(value => IsMatch(regex, value), message, () => ValidationMessages.Format);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to look like an email address (<c>local@domain.tld</c>). This is a pragmatic format check, not full RFC 5322 validation.</para>
    ///     <para xml:lang="ja">値がメールアドレスの形式（<c>local@domain.tld</c>）であることを検証する。RFC 5322への完全準拠ではなく実用上の形式チェック。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator Email(string? message = null)
    {
        AddRule(value => IsMatch(EmailRegex(), value), message, () => ValidationMessages.Email);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires every character to belong to one of the specified kinds.</para>
    ///     <para xml:lang="ja">すべての文字が指定した文字種のいずれかに該当することを検証する。</para>
    /// </summary>
    /// <param name="kinds">
    ///     <para xml:lang="en">The allowed character kinds (combined with OR).</para>
    ///     <para xml:lang="ja">許可する文字種（OR条件）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator OnlyCharacters(CharacterKinds kinds, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(kinds, CharacterKinds.None);
        AddRule(value => ConsistsOf(value, kinds), message, () => ValidationMessages.InvalidCharacters);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to satisfy a custom condition.</para>
    ///     <para xml:lang="ja">値が任意の条件を満たすことを検証する。</para>
    /// </summary>
    /// <param name="predicate">
    ///     <para xml:lang="en">Returns <see langword="true"/> when the value is valid.</para>
    ///     <para xml:lang="ja">値が妥当な場合に<see langword="true"/>を返す関数。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public StringValidator Must(Func<string, bool> predicate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        AddRule(predicate, message, () => ValidationMessages.Invalid);
        return this;
    }

    private int CountLength(string value) => _lengthUnit switch
    {
        StringLengthUnit.CodePoint => value.EnumerateRunes().Count(),
        StringLengthUnit.TextElement => new StringInfo(value).LengthInTextElements,
        _ => value.Length,
    };

    private static bool IsMatch(Regex regex, string value)
    {
        try
        {
            return regex.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    // 文字（コードポイント）単位で判定する。不正なサロゲート（ペアになっていないもの）は常に不正とする
    private static bool ConsistsOf(string value, CharacterKinds kinds)
    {
        var remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) != OperationStatus.Done || !IsAllowed(rune.Value, kinds))
            {
                return false;
            }
            remaining = remaining[consumed..];
        }
        return true;
    }

    private static bool IsAllowed(int c, CharacterKinds kinds) =>
        (kinds.HasFlag(CharacterKinds.AsciiDigit) && c is >= '0' and <= '9')
        || (kinds.HasFlag(CharacterKinds.AsciiUppercase) && c is >= 'A' and <= 'Z')
        || (kinds.HasFlag(CharacterKinds.AsciiLowercase) && c is >= 'a' and <= 'z')
        || (kinds.HasFlag(CharacterKinds.AsciiSymbol) && c is (>= '!' and <= '/') or (>= ':' and <= '@') or (>= '[' and <= '`') or (>= '{' and <= '~'))
        || (kinds.HasFlag(CharacterKinds.AsciiSpace) && c == ' ')
        || (kinds.HasFlag(CharacterKinds.Hiragana) && c is (>= 0x3041 and <= 0x3096) or (>= 0x309D and <= 0x309F) or 0x30FC)
        || (kinds.HasFlag(CharacterKinds.FullWidthKatakana) && c is >= 0x30A1 and <= 0x30FF)
        || (kinds.HasFlag(CharacterKinds.HalfWidthKatakana) && c is >= 0xFF65 and <= 0xFF9F);

    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EmailRegex();
}
