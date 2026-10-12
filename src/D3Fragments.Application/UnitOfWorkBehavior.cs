namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Saves the changes made by a successful request through <see cref="IUnitOfWork"/>. Does not save when the handler throws (typically a <see cref="DomainException"/> for a domain-level failure) or is canceled; in those cases the tracked changes are discarded and the exception is rethrown. Requests that implement <see cref="IQuery"/> are passed through unchanged. Any other request, including one that implements <see cref="IRequest{TResponse}"/> directly, is saved like a command so that changes are never lost silently.</para>
///     <para xml:lang="ja">成功したリクエストの変更を<see cref="IUnitOfWork"/>経由で保存するPipeline Behavior。Handlerが例外を送出した場合（ドメインレベルの失敗は通常<see cref="DomainException"/>）、キャンセルされた場合は保存せず、追跡中の変更を破棄して例外を再スローする。<see cref="IQuery"/>を実装したリクエストはそのまま通過させる。それ以外のリクエスト（<see cref="IRequest{TResponse}"/>を直接実装したものを含む）は、変更が黙って失われないようCommandと同様に保存する。</para>
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public int Order => 300;

    /// <inheritdoc />
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IQuery)
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var response = await next(cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
        catch
        {
            unitOfWork.DiscardChanges();
            throw;
        }
    }
}
