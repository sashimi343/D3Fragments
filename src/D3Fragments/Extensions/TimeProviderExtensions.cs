namespace D3Fragments.Extensions;

/// <summary>
///     <para xml:lang="en">Provides extension methods for <see cref="TimeProvider"/>.</para>
///     <para xml:lang="ja"><see cref="TimeProvider"/>に対する拡張メソッドを提供する。</para>
/// </summary>
public static class TimeProviderExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Gets today's date in the local time zone of <paramref name="timeProvider"/> (<see cref="TimeProvider.LocalTimeZone"/>).</para>
    ///     <para xml:lang="ja"><paramref name="timeProvider"/>のローカルタイムゾーン（<see cref="TimeProvider.LocalTimeZone"/>）における今日の日付を取得する。</para>
    /// </summary>
    /// <param name="timeProvider">
    ///     <para xml:lang="en">The time provider.</para>
    ///     <para xml:lang="ja">時刻プロバイダー。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The date part of <see cref="TimeProvider.GetLocalNow"/>.</para>
    ///     <para xml:lang="ja"><see cref="TimeProvider.GetLocalNow"/>の日付部分。</para>
    /// </returns>
    public static DateOnly GetLocalToday(this TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        return DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
    }
}
