using System.Globalization;
using System.Numerics;

namespace D3Fragments.Validations;

/// <summary>
///     <para xml:lang="en">Defines and applies validation rules to a number (nullable numbers such as <c>int?</c> are supported). For floating-point types, <c>NaN</c> fails every comparison rule.</para>
///     <para xml:lang="ja">数値に対するバリデーションルールを定義・適用するクラス（<c>int?</c>等のnullableにも対応）。浮動小数点型の<c>NaN</c>は比較系ルールをすべて満たさない。</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The numeric type to validate.</para>
///     <para xml:lang="ja">検証対象の数値型。</para>
/// </typeparam>
public sealed class NumberValidator<T> : ValueValidatorBase<T>
    where T : struct, INumber<T>
{
    /// <inheritdoc cref="ValidatorBase{T}(string, string?)" />
    public NumberValidator(string displayName, string? target = null) : base(displayName, target)
    {
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be non-null.</para>
    ///     <para xml:lang="ja">値がnullでないことを検証する。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public NumberValidator<T> Required(string? message = null)
    {
        SetRequired(message, () => ValidationMessages.Required);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be greater than or equal to <paramref name="min"/>.</para>
    ///     <para xml:lang="ja">値が<paramref name="min"/>以上であることを検証する。</para>
    /// </summary>
    /// <param name="min">
    ///     <para xml:lang="en">The minimum value (inclusive).</para>
    ///     <para xml:lang="ja">最小値（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="min"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="min"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public NumberValidator<T> Min(T min, string? message = null)
    {
        AddRule(value => value >= min, message, () => ValidationMessages.NumberMin, min);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be less than or equal to <paramref name="max"/>.</para>
    ///     <para xml:lang="ja">値が<paramref name="max"/>以下であることを検証する。</para>
    /// </summary>
    /// <param name="max">
    ///     <para xml:lang="en">The maximum value (inclusive).</para>
    ///     <para xml:lang="ja">最大値（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="max"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="max"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public NumberValidator<T> Max(T max, string? message = null)
    {
        AddRule(value => value <= max, message, () => ValidationMessages.NumberMax, max);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be between <paramref name="min"/> and <paramref name="max"/> (inclusive).</para>
    ///     <para xml:lang="ja">値が<paramref name="min"/>以上<paramref name="max"/>以下であることを検証する。</para>
    /// </summary>
    /// <param name="min">
    ///     <para xml:lang="en">The minimum value (inclusive).</para>
    ///     <para xml:lang="ja">最小値（この値を含む）。</para>
    /// </param>
    /// <param name="max">
    ///     <para xml:lang="en">The maximum value (inclusive).</para>
    ///     <para xml:lang="ja">最大値（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="min"/>, <c>{2}</c> = <paramref name="max"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="min"/>、<c>{2}</c>＝<paramref name="max"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public NumberValidator<T> Range(T min, T max, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        AddRule(value => value >= min && value <= max, message, () => ValidationMessages.NumberRange, min, max);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be greater than 0.</para>
    ///     <para xml:lang="ja">値が0より大きいことを検証する。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public NumberValidator<T> Positive(string? message = null)
    {
        AddRule(value => value > T.Zero, message, () => ValidationMessages.Positive);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be 0 or greater.</para>
    ///     <para xml:lang="ja">値が0以上であることを検証する。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public NumberValidator<T> NonNegative(string? message = null)
    {
        AddRule(value => value >= T.Zero, message, () => ValidationMessages.NonNegative);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the number of integer digits and decimal places to be within the limits (e.g. for a <c>decimal(p, s)</c> column, use <c>Digits(p - s, s)</c>). Trailing zeros in the fractional part are ignored. The value is evaluated after conversion to <see cref="decimal"/>; a value that cannot be converted is invalid.</para>
    ///     <para xml:lang="ja">整数部・小数部の桁数が上限以内であることを検証する（<c>decimal(p, s)</c>列であれば<c>Digits(p - s, s)</c>）。小数部末尾の0は無視する。値は<see cref="decimal"/>に変換して評価し、変換できない値は不正とする。</para>
    /// </summary>
    /// <param name="integerDigits">
    ///     <para xml:lang="en">The maximum number of integer digits.</para>
    ///     <para xml:lang="ja">整数部の最大桁数。</para>
    /// </param>
    /// <param name="fractionDigits">
    ///     <para xml:lang="en">The maximum number of decimal places.</para>
    ///     <para xml:lang="ja">小数部の最大桁数。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="integerDigits"/>, <c>{2}</c> = <paramref name="fractionDigits"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="integerDigits"/>、<c>{2}</c>＝<paramref name="fractionDigits"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public NumberValidator<T> Digits(int integerDigits, int fractionDigits, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(integerDigits);
        ArgumentOutOfRangeException.ThrowIfNegative(fractionDigits);
        AddRule(value => HasDigitsWithin(value, integerDigits, fractionDigits), message, () => ValidationMessages.Digits, integerDigits, fractionDigits);
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
    public NumberValidator<T> Must(Func<T, bool> predicate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        AddRule(predicate, message, () => ValidationMessages.Invalid);
        return this;
    }

    private static bool HasDigitsWithin(T value, int integerDigits, int fractionDigits)
    {
        decimal number;
        try
        {
            number = decimal.CreateChecked(value);
        }
        catch (OverflowException)
        {
            return false;
        }

        // 末尾の0を落とした表記から整数部・小数部の桁数を数える（整数部の"0"は0桁とする）
        var parts = Math.Abs(number).ToString("0.############################", CultureInfo.InvariantCulture).Split('.');
        var actualIntegerDigits = parts[0] == "0" ? 0 : parts[0].Length;
        var actualFractionDigits = parts.Length > 1 ? parts[1].Length : 0;
        return actualIntegerDigits <= integerDigits && actualFractionDigits <= fractionDigits;
    }
}
