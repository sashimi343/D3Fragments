using D3Fragments;

namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Saves the changes made by a successful command through <see cref="IUnitOfWork"/>. Does not save when the handler throws (typically a <see cref="DomainException"/> for a domain-level failure) or is canceled; in those cases the tracked changes are discarded and the exception is rethrown. Queries are passed through unchanged.</para>
///     <para xml:lang="ja">成功したCommandの変更を<see cref="IUnitOfWork"/>経由で保存するPipeline Behavior。Handlerが例外を送出した場合（ドメインレベルの失敗は通常<see cref="DomainException"/>）、キャンセルされた場合は保存せず、追跡中の変更を破棄して例外を再スローする。Queryはそのまま通過させる。</para>
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
        if (request is not ICommand)
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
