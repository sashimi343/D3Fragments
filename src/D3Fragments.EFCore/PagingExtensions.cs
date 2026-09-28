using Microsoft.EntityFrameworkCore;

namespace D3Fragments.EFCore;

/// <summary>
///     <para xml:lang="en">Extension methods that provide a single, shared paging implementation for EF Core queries so each Query Service does not repeat the same page-number/page-size validation.</para>
///     <para xml:lang="ja">EF Coreクエリに対する一本化されたページング実装を提供する拡張メソッド。各Query Serviceでページ番号・ページサイズの検証を重複実装する必要がなくなる。</para>
/// </summary>
public static class PagingExtensions
{
    /// <summary>
    ///     <para xml:lang="en">The default maximum page size applied when the caller does not specify a smaller limit.</para>
    ///     <para xml:lang="ja">呼び出し側が別の上限を指定しない場合に適用される既定の最大ページサイズ。</para>
    /// </summary>
    public const int DefaultMaxPageSize = 200;

    /// <summary>
    ///     <para xml:lang="en">Retrieves a single page of <paramref name="query"/>. Validates <paramref name="pageNo"/> and <paramref name="pageSize"/> before issuing any database query. A page number past the last page is not an error and yields an empty page. The caller is responsible for applying a stable <c>OrderBy</c> (including a unique key) to <paramref name="query"/> before calling this method; this method does not impose an ordering.</para>
    ///     <para xml:lang="ja"><paramref name="query"/>の1ページ分を取得する。DBへの問い合わせを行う前に<paramref name="pageNo"/>と<paramref name="pageSize"/>を検証する。最終ページを超えるページ番号はエラーではなく空ページを返す。安定したページングに必要な一意キーを含む<c>OrderBy</c>の指定は呼び出し側の責務であり、このメソッド自体はソート順を強制しない。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the items.</para>
    ///     <para xml:lang="ja">データの型。</para>
    /// </typeparam>
    /// <param name="query">
    ///     <para xml:lang="en">The query to page. Should already have an <c>OrderBy</c> applied.</para>
    ///     <para xml:lang="ja">ページング対象のクエリ。あらかじめ<c>OrderBy</c>が適用されている必要がある。</para>
    /// </param>
    /// <param name="pageNo">
    ///     <para xml:lang="en">The page number, starting at <see cref="IPager.StartPageNo"/>.</para>
    ///     <para xml:lang="ja"><see cref="IPager.StartPageNo"/>を起点とするページ番号。</para>
    /// </param>
    /// <param name="pageSize">
    ///     <para xml:lang="en">The maximum number of items per page.</para>
    ///     <para xml:lang="ja">1ページあたりの最大データ件数。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    /// <param name="maxPageSize">
    ///     <para xml:lang="en">The maximum allowed page size. Defaults to <see cref="DefaultMaxPageSize"/>.</para>
    ///     <para xml:lang="ja">許容する最大ページサイズ。既定値は<see cref="DefaultMaxPageSize"/>。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A success result holding the requested page, or a failure result when <paramref name="pageNo"/> or <paramref name="pageSize"/> is invalid.</para>
    ///     <para xml:lang="ja">成功時は要求されたページを保持する結果、<paramref name="pageNo"/>または<paramref name="pageSize"/>が不正な場合は失敗結果。</para>
    /// </returns>
    public static async Task<Result<Pager<T>>> ToPagedListAsync<T>(
        this IQueryable<T> query,
        int pageNo,
        int pageSize,
        CancellationToken cancellationToken,
        int maxPageSize = DefaultMaxPageSize)
    {
        if (pageNo < IPager.StartPageNo)
        {
            return Result<Pager<T>>.Failure(Error.Validation(string.Format(ExceptionMessages.Error_PageNoMustBeAtLeastStartPageNo, IPager.StartPageNo)));
        }

        if (pageSize <= 0)
        {
            return Result<Pager<T>>.Failure(Error.Validation(ExceptionMessages.Error_PageSizeMustBePositive));
        }

        if (pageSize > maxPageSize)
        {
            return Result<Pager<T>>.Failure(Error.Validation(string.Format(ExceptionMessages.Error_PageSizeExceedsMaximum, maxPageSize)));
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        if (totalCount == 0)
        {
            return Pager<T>.Empty(pageSize);
        }

        var skip = checked((pageNo - 1) * pageSize);
        var items = await query.Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);

        return new Pager<T>(items, pageNo, pageSize, totalCount);
    }
}
