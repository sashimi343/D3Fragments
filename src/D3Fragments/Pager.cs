namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents a page of <typeparamref name="T"/> items retrieved through pagination.</para>
///     <para xml:lang="ja">ページネーション機能により分割取得した<typeparamref name="T"/>型のデータを格納するコレクション。</para>
/// </summary>
public sealed class Pager<T> : IPager<T>
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="Pager{T}"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="Pager{T}"/> を初期化する。</para>
    /// </summary>
    /// <param name="items">
    ///     <para xml:lang="en">The items contained in the current page.</para>
    ///     <para xml:lang="ja">現在のページ内に含まれるデータの一覧。</para>
    /// </param>
    /// <param name="pageNo">
    ///     <para xml:lang="en">The current page number.</para>
    ///     <para xml:lang="ja">現在のページ番号。</para>
    /// </param>
    /// <param name="pageSize">
    ///     <para xml:lang="en">The maximum number of items per page.</para>
    ///     <para xml:lang="ja">1ページあたりの最大データ件数。</para>
    /// </param>
    /// <param name="totalCount">
    ///     <para xml:lang="en">The total number of items across all pages.</para>
    ///     <para xml:lang="ja">現在のページ以外も含むすべてのページに存在するデータの件数。</para>
    /// </param>
    public Pager(IEnumerable<T> items, int pageNo, int pageSize, int totalCount)
    {
        Items = items.ToList();
        PageNo = pageNo;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    /// <inheritdoc/>
    public int PageNo { get; }

    /// <inheritdoc/>
    public int PageSize { get; }

    /// <inheritdoc/>
    public int TotalCount { get; }

    /// <inheritdoc/>
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <inheritdoc/>
    public bool HasPrevPage => PageNo > IPager.StartPageNo;

    /// <inheritdoc/>
    public bool HasNextPage => PageNo < TotalPages;

    /// <inheritdoc/>
    public int? PrevPage => HasPrevPage ? PageNo - 1 : null;

    /// <inheritdoc/>
    public int? NextPage => HasNextPage ? PageNo + 1 : null;

    /// <inheritdoc/>
    public IReadOnlyList<T> Items { get; }

    /// <summary>
    ///     <para xml:lang="en">Creates an empty <see cref="Pager{T}"/> with the specified page size.</para>
    ///     <para xml:lang="ja">指定されたページサイズで、データが0件のページャを生成する。</para>
    /// </summary>
    public static Pager<T> Empty(int pageSize) => new([], IPager.StartPageNo, pageSize, 0);
}
