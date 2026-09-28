namespace D3Fragments.Extensions;

/// <summary>
///     <para xml:lang="en">Provides extension methods for <see cref="IEnumerable{T}"/>.</para>
///     <para xml:lang="ja"><see cref="IEnumerable{T}"/>に対する拡張メソッドを提供する。</para>
/// </summary>
public static class EnumerableExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Filters and projects the elements in one step: applies <paramref name="selector"/> to each element and returns only the results it marks as successful. Useful with <c>TryParse</c>-style methods.</para>
    ///     <para xml:lang="ja">フィルタリングと型変換を同時に行う。各要素に<paramref name="selector"/>を適用し、成功とされた結果のみを返す。<c>TryParse</c>系メソッドとの組み合わせに便利。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the source elements.</para>
    ///     <para xml:lang="ja">元のコレクションの要素型。</para>
    /// </typeparam>
    /// <typeparam name="TResult">
    ///     <para xml:lang="en">The type of the projected elements.</para>
    ///     <para xml:lang="ja">変換後の要素型。</para>
    /// </typeparam>
    /// <param name="source">
    ///     <para xml:lang="en">The source collection.</para>
    ///     <para xml:lang="ja">元のコレクション。</para>
    /// </param>
    /// <param name="selector">
    ///     <para xml:lang="en">Returns whether the element is kept, and its projected value.</para>
    ///     <para xml:lang="ja">要素を残すかどうかと変換後の値を返す関数。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The projected values of the elements for which <paramref name="selector"/> succeeded.</para>
    ///     <para xml:lang="ja"><paramref name="selector"/>が成功した要素の変換後の値からなるコレクション。</para>
    /// </returns>
    /// <example>
    /// <code>
    /// IEnumerable&lt;int&gt; ids = values.SelectWhere(x => (int.TryParse(x, out var id), id));
    /// </code>
    /// </example>
    public static IEnumerable<TResult> SelectWhere<T, TResult>(this IEnumerable<T> source, Func<T, (bool Success, TResult Result)> selector)
    {
        // yieldを含むメソッドは列挙開始まで実行されないため、引数チェックを外側で即時に行う
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        return Iterate(source, selector);

        static IEnumerable<TResult> Iterate(IEnumerable<T> source, Func<T, (bool Success, TResult Result)> selector)
        {
            foreach (var item in source)
            {
                var (success, result) = selector(item);
                if (success)
                {
                    yield return result;
                }
            }
        }
    }
}
