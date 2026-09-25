namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents a page of items retrieved through pagination.</para>
///     <para xml:lang="ja">ページネーション機能により分割取得したデータを格納するコレクションのインターフェース。</para>
/// </summary>
public interface IPager
{
    /// <summary>
    ///     <para xml:lang="en">The first page number.</para>
    ///     <para xml:lang="ja">開始ページ番号。</para>
    /// </summary>
    const int StartPageNo = 1;

    /// <summary>
    ///     <para xml:lang="en">Gets the current page number.</para>
    ///     <para xml:lang="ja">現在のページ番号を取得する。</para>
    /// </summary>
    int PageNo { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the maximum number of items per page.</para>
    ///     <para xml:lang="ja">1ページあたりの最大データ件数を取得する。</para>
    /// </summary>
    int PageSize { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the total number of items across all pages.</para>
    ///     <para xml:lang="ja">現在のページ以外も含む全てのページに存在するデータ件数を取得する。</para>
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the total number of pages.</para>
    ///     <para xml:lang="ja">現在のページを含む全てのページ数を取得する。</para>
    /// </summary>
    int TotalPages { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the previous page number, or <c>null</c> when there is no previous page.</para>
    ///     <para xml:lang="ja">現在のページの1ページ前のページ番号を取得する。前のページが存在しない場合は <c>null</c>。</para>
    /// </summary>
    int? PrevPage { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the next page number, or <c>null</c> when there is no next page.</para>
    ///     <para xml:lang="ja">現在のページの1ページ後のページ番号を取得する。次のページが存在しない場合は <c>null</c>。</para>
    /// </summary>
    int? NextPage { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether a previous page exists.</para>
    ///     <para xml:lang="ja">前のページが存在するかどうかの値を取得する。</para>
    /// </summary>
    bool HasPrevPage { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether a next page exists.</para>
    ///     <para xml:lang="ja">次のページが存在するかどうかの値を取得する。</para>
    /// </summary>
    bool HasNextPage { get; }
}

/// <summary>
///     <para xml:lang="en">Represents a page of <typeparamref name="T"/> items retrieved through pagination.</para>
///     <para xml:lang="ja"><typeparamref name="T"/>型のデータを格納するページャのインターフェース。</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The type of the items.</para>
///     <para xml:lang="ja">データの型。</para>
/// </typeparam>
public interface IPager<T> : IPager
{
    /// <summary>
    ///     <para xml:lang="en">Gets the items contained in the current page.</para>
    ///     <para xml:lang="ja">現在のページ内に含まれるデータの一覧を取得する。</para>
    /// </summary>
    IReadOnlyList<T> Items { get; }
}
