namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Extension methods for <see cref="IRequestExecutor{TRequest, TResponse}"/>.</para>
///     <para xml:lang="ja"><see cref="IRequestExecutor{TRequest, TResponse}"/>の拡張メソッド。</para>
/// </summary>
public static class RequestExecutorExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Executes the request and converts a thrown <see cref="DomainException"/> into a failure result. Other exceptions are propagated unchanged, so the caller does not need a <c>try-catch</c> that might accidentally swallow non-domain exceptions.</para>
    ///     <para xml:lang="ja">リクエストを実行し、送出された<see cref="DomainException"/>を失敗結果に変換する。それ以外の例外はそのまま伝播するため、呼び出し側で非ドメイン例外まで握りつぶしかねない<c>try-catch</c>を書く必要がない。</para>
    /// </summary>
    /// <typeparam name="TRequest">
    ///     <para xml:lang="en">The request type.</para>
    ///     <para xml:lang="ja">リクエストの型。</para>
    /// </typeparam>
    /// <typeparam name="TResponse">
    ///     <para xml:lang="en">The response type.</para>
    ///     <para xml:lang="ja">レスポンスの型。</para>
    /// </typeparam>
    /// <param name="executor">
    ///     <para xml:lang="en">The executor.</para>
    ///     <para xml:lang="ja">実行するExecutor。</para>
    /// </param>
    /// <param name="request">
    ///     <para xml:lang="en">The request.</para>
    ///     <para xml:lang="ja">リクエスト。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A success result holding the response, or a failure result holding the errors of the <see cref="DomainException"/>.</para>
    ///     <para xml:lang="ja">成功時はレスポンスを保持する結果、<see cref="DomainException"/>が送出された場合はそのエラーを保持する失敗結果。</para>
    /// </returns>
    public static async Task<Result<TResponse>> TryExecuteAsync<TRequest, TResponse>(
        this IRequestExecutor<TRequest, TResponse> executor,
        TRequest request,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(executor);
        try
        {
            return Result<TResponse>.Success(await executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false));
        }
        catch (DomainException exception)
        {
            return Result<TResponse>.Failure(exception.Errors);
        }
    }
}
