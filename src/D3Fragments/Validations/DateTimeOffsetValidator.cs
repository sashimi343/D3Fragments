namespace D3Fragments.Validations;

/// <summary>
///     <para xml:lang="en">Defines and applies validation rules to a <see cref="DateTimeOffset"/> (nullable <c>DateTimeOffset?</c> is supported).</para>
///     <para xml:lang="ja"><see cref="DateTimeOffset"/>に対するバリデーションルールを定義・適用するクラス（<c>DateTimeOffset?</c>にも対応）。</para>
/// </summary>
public sealed class DateTimeOffsetValidator : ValueValidatorBase<DateTimeOffset>
{
    /// <inheritdoc cref="ValidatorBase{T}(string, string?)" />
    public DateTimeOffsetValidator(string displayName, string? target = null) : base(displayName, target)
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
    public DateTimeOffsetValidator Required(string? message = null)
    {
        SetRequired(message, () => ValidationMessages.Required);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be on or after <paramref name="min"/>.</para>
    ///     <para xml:lang="ja">日時が<paramref name="min"/>以降であることを検証する。</para>
    /// </summary>
    /// <param name="min">
    ///     <para xml:lang="en">The minimum date and time (inclusive).</para>
    ///     <para xml:lang="ja">最小日時（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="min"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="min"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateTimeOffsetValidator Min(DateTimeOffset min, string? message = null)
    {
        AddRule(value => value >= min, message, () => ValidationMessages.DateTimeMin, min);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be on or before <paramref name="max"/>.</para>
    ///     <para xml:lang="ja">日時が<paramref name="max"/>以前であることを検証する。</para>
    /// </summary>
    /// <param name="max">
    ///     <para xml:lang="en">The maximum date and time (inclusive).</para>
    ///     <para xml:lang="ja">最大日時（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="max"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="max"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateTimeOffsetValidator Max(DateTimeOffset max, string? message = null)
    {
        AddRule(value => value <= max, message, () => ValidationMessages.DateTimeMax, max);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be between <paramref name="min"/> and <paramref name="max"/> (inclusive).</para>
    ///     <para xml:lang="ja">日時が<paramref name="min"/>から<paramref name="max"/>の間であることを検証する。</para>
    /// </summary>
    /// <param name="min">
    ///     <para xml:lang="en">The minimum date and time (inclusive).</para>
    ///     <para xml:lang="ja">最小日時（この値を含む）。</para>
    /// </param>
    /// <param name="max">
    ///     <para xml:lang="en">The maximum date and time (inclusive).</para>
    ///     <para xml:lang="ja">最大日時（この値を含む）。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name, <c>{1}</c> = <paramref name="min"/>, <c>{2}</c> = <paramref name="max"/>), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名、<c>{1}</c>＝<paramref name="min"/>、<c>{2}</c>＝<paramref name="max"/>）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateTimeOffsetValidator Range(DateTimeOffset min, DateTimeOffset max, string? message = null)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(min, max);
        AddRule(value => value >= min && value <= max, message, () => ValidationMessages.DateTimeRange, min, max);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be before the current time of <paramref name="timeProvider"/> at validation time.</para>
    ///     <para xml:lang="ja">日時が検証実行時の<paramref name="timeProvider"/>の現在日時より前（過去）であることを検証する。</para>
    /// </summary>
    /// <param name="timeProvider">
    ///     <para xml:lang="en">The time provider that supplies the current time.</para>
    ///     <para xml:lang="ja">現在日時を提供するTimeProvider。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateTimeOffsetValidator Past(TimeProvider timeProvider, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        AddRule(value => value < timeProvider.GetUtcNow(), message, () => ValidationMessages.DateTimePast);
        return this;
    }

    /// <summary>
    ///     <para xml:lang="en">Requires the value to be after the current time of <paramref name="timeProvider"/> at validation time.</para>
    ///     <para xml:lang="ja">日時が検証実行時の<paramref name="timeProvider"/>の現在日時より後（未来）であることを検証する。</para>
    /// </summary>
    /// <param name="timeProvider">
    ///     <para xml:lang="en">The time provider that supplies the current time.</para>
    ///     <para xml:lang="ja">現在日時を提供するTimeProvider。</para>
    /// </param>
    /// <param name="message">
    ///     <para xml:lang="en">A custom message (<c>{0}</c> = display name), or <see langword="null"/> for the default.</para>
    ///     <para xml:lang="ja">カスタムメッセージ（<c>{0}</c>＝表示名）。<see langword="null"/>の場合は既定メッセージ。</para>
    /// </param>
    public DateTimeOffsetValidator Future(TimeProvider timeProvider, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        AddRule(value => value > timeProvider.GetUtcNow(), message, () => ValidationMessages.DateTimeFuture);
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
    public DateTimeOffsetValidator Must(Func<DateTimeOffset, bool> predicate, string? message = null)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        AddRule(predicate, message, () => ValidationMessages.Invalid);
        return this;
    }
}
