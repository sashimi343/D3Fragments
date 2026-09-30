namespace D3Fragments.Testing;

/// <summary>
///     <para xml:lang="en">Provides values that are unique within the test process.</para>
///     <para xml:lang="ja">テストプロセス内で一意な値を提供する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Use these values for properties with unique constraints so that tests running in parallel do not conflict.</para>
///     <para xml:lang="ja">一意制約を持つプロパティに使用することで、並列実行されるテスト同士の衝突を防ぐ。</para>
/// </remarks>
public static class TestData
{
    private static int _sequence;

    /// <summary>
    ///     <para xml:lang="en">Returns a string that consists of the prefix and a sequence number, such as <c>DEV-1</c>.</para>
    ///     <para xml:lang="ja">接頭辞と連番からなる文字列（<c>DEV-1</c>など）を返す。</para>
    /// </summary>
    /// <param name="prefix">
    ///     <para xml:lang="en">The prefix of the string.</para>
    ///     <para xml:lang="ja">文字列の接頭辞。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A string that is unique within the test process.</para>
    ///     <para xml:lang="ja">テストプロセス内で一意な文字列。</para>
    /// </returns>
    public static string Unique(string prefix) => $"{prefix}-{UniqueNumber()}";

    /// <summary>
    ///     <para xml:lang="en">Returns a positive sequence number.</para>
    ///     <para xml:lang="ja">正の連番を返す。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A number that is unique within the test process.</para>
    ///     <para xml:lang="ja">テストプロセス内で一意な数値。</para>
    /// </returns>
    public static int UniqueNumber() => Interlocked.Increment(ref _sequence);
}
