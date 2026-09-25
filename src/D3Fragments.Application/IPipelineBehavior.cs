namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Delegate that invokes the next step in the pipeline (either the next behavior or the handler itself).</para>
///     <para xml:lang="ja">Pipelineの次のステップ（次のBehaviorまたはHandler自身）を呼び出すデリゲート。</para>
/// </summary>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
/// <param name="cancellationToken">
///     <para xml:lang="en">The cancellation token.</para>
///     <para xml:lang="ja">キャンセルトークン。</para>
/// </param>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);

/// <summary>
///     <para xml:lang="en">A cross-cutting concern that wraps request handling, such as transaction management or logging.</para>
///     <para xml:lang="ja">トランザクション制御やロギング等、リクエスト処理を横断する関心事を表すインターフェース。</para>
/// </summary>
/// <typeparam name="TRequest">
///     <para xml:lang="en">The type of the request.</para>
///     <para xml:lang="ja">リクエストの型。</para>
/// </typeparam>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>
    ///     <para xml:lang="en">Gets the execution order of this behavior. A behavior with a smaller value runs further to the outside of the pipeline. Determines pipeline order instead of DI registration order.</para>
    ///     <para xml:lang="ja">このBehaviorの実行順序を取得する。値が小さいBehaviorほどPipelineの外側で実行される。DI登録順ではなくこの値でPipelineの順序が決まる。</para>
    /// </summary>
    int Order { get; }

    /// <summary>
    ///     <para xml:lang="en">Handles the request, optionally invoking <paramref name="next"/> to continue the pipeline.</para>
    ///     <para xml:lang="ja">リクエストを処理する。<paramref name="next"/>を呼び出すことでPipelineの後続処理を継続する。</para>
    /// </summary>
    /// <param name="request">
    ///     <para xml:lang="en">The request being handled.</para>
    ///     <para xml:lang="ja">処理対象のリクエスト。</para>
    /// </param>
    /// <param name="next">
    ///     <para xml:lang="en">The delegate that invokes the next step in the pipeline.</para>
    ///     <para xml:lang="ja">Pipelineの次のステップを呼び出すデリゲート。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
