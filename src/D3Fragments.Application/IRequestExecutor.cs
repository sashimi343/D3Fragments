namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Executes a request of type <typeparamref name="TRequest"/> through the handler and its pipeline behaviors.</para>
///     <para xml:lang="ja">型<typeparamref name="TRequest"/>のリクエストをHandlerおよびPipeline Behavior経由で実行するインターフェース。</para>
/// </summary>
/// <typeparam name="TRequest">
///     <para xml:lang="en">The type of the request.</para>
///     <para xml:lang="ja">リクエストの型。</para>
/// </typeparam>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
public interface IRequestExecutor<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    ///     <para xml:lang="en">Executes the request asynchronously.</para>
    ///     <para xml:lang="ja">リクエストを非同期的に実行する。</para>
    /// </summary>
    /// <param name="request">
    ///     <para xml:lang="en">The request to execute.</para>
    ///     <para xml:lang="ja">実行対象のリクエスト。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken);
}
