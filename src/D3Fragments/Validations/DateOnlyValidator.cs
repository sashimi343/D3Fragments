namespace D3Fragments.Validations;

/// <summary>
///     <para xml:lang="en">Defines and applies validation rules to a <see cref="DateOnly"/> (nullable <c>DateOnly?</c> is supported).</para>
///     <para xml:lang="ja"><see cref="DateOnly"/>に対するバリデーションルールを定義・適用するクラス（<c>DateOnly?</c>にも対応）。</para>
/// </summary>
public sealed class DateOnlyValidator : ValueValidatorBase<DateOnly>
{
    /// <inheritdoc cref="ValidatorBase{T}(string, string?)" />
    public DateOnlyValidator(string displayName, string? target = null) : base(displayName, target)
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
    public DateOnlyValidator Required(string? message = null)
    {
        SetRequired(message, () => ValidationMessages.Required);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the date to be on or after <paramref name="min"/>.</para>
    ///     <para xml:lang="ja">日付が<paramref name="min"/>以降であることを検証する。</para>
    /// </summary>
    /// <param name="min">
    ///     <para xml:lang="en">The minimum date (inclusive).</para>
    ///     <para xml:lang="ja">最小日付（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="min"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="min"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateOnlyValidator Min(DateOnly min, string? message = null)
    {
        AddRule(value => value >= min, message, () => ValidationMessages.DateMin, min);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the date to be on or before <paramref name="max"/>.</para>
    ///     <para xml:lang="ja">日付が<paramref name="max"/>以前であることを検証する。</para>
    /// </summary>
    /// <param name="max">
    ///     <para xml:lang="en">The maximum date (inclusive).</para>
    ///     <para xml:lang="ja">最大日付（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="max"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="max"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateOnlyValidator Max(DateOnly max, string? message = null)
    {
        AddRule(value => value <= max, message, () => ValidationMessages.DateMax, max);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the date to be between <paramref name="min"/> and <paramref name="max"/> (inclusive).</para>
    ///     <para xml:lang="ja">日付が<paramref name="min"/>から<paramref name="max"/>の間であることを検証する。</para>
    /// </summary>
    /// <param name="min">
    ///     <para xml:lang="en">The minimum date (inclusive).</para>
    ///     <para xml:lang="ja">最小日付（この値を含む）。</para>
    /// </param>
    /// <param name="max">
    ///     <para xml:lang="en">The maximum date (inclusive).</para>
    ///     <para xml:lang="ja">最大日付（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="min"/>, <c>{2}</c> = <paramref name="max"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="min"/>、<c>{2}</c>＝<paramref name="max"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateOnlyValidator Range(DateOnly min, DateOnly max, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        AddRule(value => value >= min && value <= max, message, () => ValidationMessages.DateRange, min, max);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the date to be before today. "Today" is the local date of <paramref name="timeProvider"/> at validation time.</para>
    ///     <para xml:lang="ja">日付が今日より前（過去）であることを検証する。「今日」は検証実行時の<paramref name="timeProvider"/>のローカル日付。</para>
    /// </summary>
    /// <param name="timeProvider">
    ///     <para xml:lang="en">The time provider that supplies the current date.</para>
    ///     <para xml:lang="ja">現在日付を提供するTimeProvider。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateOnlyValidator Past(TimeProvider timeProvider, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        AddRule(value => value < Today(timeProvider), message, () => ValidationMessages.DatePast);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the date to be after today. "Today" is the local date of <paramref name="timeProvider"/> at validation time.</para>
    ///     <para xml:lang="ja">日付が今日より後（未来）であることを検証する。「今日」は検証実行時の<paramref name="timeProvider"/>のローカル日付。</para>
    /// </summary>
    /// <param name="timeProvider">
    ///     <para xml:lang="en">The time provider that supplies the current date.</para>
    ///     <para xml:lang="ja">現在日付を提供するTimeProvider。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateOnlyValidator Future(TimeProvider timeProvider, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        AddRule(value => value > Today(timeProvider), message, () => ValidationMessages.DateFuture);
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
    public DateOnlyValidator Must(Func<DateOnly, bool> predicate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        AddRule(predicate, message, () => ValidationMessages.Invalid);
        return this;
    }

    private static DateOnly Today(TimeProvider timeProvider) => DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
}
