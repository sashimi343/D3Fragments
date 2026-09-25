namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">The single open-generic <see cref="IRequestExecutor{TRequest, TResponse}"/> implementation used for every request type. No per-request executor is needed.</para>
///     <para xml:lang="ja">全リクエスト型に対して共通で使われる、open genericな唯一の<see cref="IRequestExecutor{TRequest, TResponse}"/>実装。リクエストごとに個別のExecutorを用意する必要はない。</para>
/// </summary>
/// <typeparam name="TRequest">
///     <para xml:lang="en">The type of the request.</para>
///     <para xml:lang="ja">リクエストの型。</para>
/// </typeparam>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
public sealed class RequestExecutor<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> handler,
    IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors)
    : IRequestExecutor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
    {
        RequestHandlerDelegate<TResponse> next = ct => handler.HandleAsync(request, ct);

        foreach (var behavior in behaviors.OrderByDescending(x => x.Order))
        {
            var current = next;
            next = ct => behavior.HandleAsync(request, current, ct);
        }

        return next(cancellationToken);
    }
}
