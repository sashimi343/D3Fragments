using Microsoft.Extensions.Time.Testing;

namespace D3Fragments.Testing;

/// <summary>
///     <para xml:lang="en">Provides the fixed reference time for tests.</para>
///     <para xml:lang="ja">テストで使用する固定の基準時刻を提供する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Use <see cref="Now"/> for the <c>createdAt</c> argument of entities created in tests, so that the test data matches the time of <see cref="CreateProvider"/>.</para>
///     <para xml:lang="ja">テストで生成するエンティティの<c>createdAt</c>引数に<see cref="Now"/>を使用すると、テストデータの時刻が<see cref="CreateProvider"/>の時刻と一致する。</para>
/// </remarks>
public static class TestTime
{
    /// <summary>
    ///     <para xml:lang="en">Gets the fixed reference time (2026-01-01T00:00:00Z).</para>
    ///     <para xml:lang="ja">固定の基準時刻（2026-01-01T00:00:00Z）を取得する。</para>
    /// </summary>
    public static DateTimeOffset Now { get; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    ///     <para xml:lang="en">Creates a <see cref="FakeTimeProvider"/> whose current time is <see cref="Now"/>.</para>
    ///     <para xml:lang="ja">現在時刻が<see cref="Now"/>の<see cref="FakeTimeProvider"/>を生成する。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A new <see cref="FakeTimeProvider"/> instance.</para>
    ///     <para xml:lang="ja">新しい<see cref="FakeTimeProvider"/>インスタンス。</para>
    /// </returns>
    public static FakeTimeProvider CreateProvider() => new(Now);
}
