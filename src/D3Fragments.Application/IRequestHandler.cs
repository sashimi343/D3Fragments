namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Handles a request of type <typeparamref name="TRequest"/> and produces <typeparamref name="TResponse"/>.</para>
///     <para xml:lang="ja">型<typeparamref name="TRequest"/>のリクエストを処理し、<typeparamref name="TResponse"/>を生成するハンドラーのインターフェース。</para>
/// </summary>
/// <typeparam name="TRequest">
///     <para xml:lang="en">The type of the request.</para>
///     <para xml:lang="ja">リクエストの型。</para>
/// </typeparam>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    ///     <para xml:lang="en">Handles the request asynchronously.</para>
    ///     <para xml:lang="ja">リクエストを非同期的に処理する。</para>
    /// </summary>
    /// <param name="request">
    ///     <para xml:lang="en">The request to handle.</para>
    ///     <para xml:lang="ja">処理対象のリクエスト。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
