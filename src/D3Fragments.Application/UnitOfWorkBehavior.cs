using D3Fragments;

namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Saves the changes made by a successful command through <see cref="IUnitOfWork"/>. Does not save when the handler returns a failed <see cref="Result"/>, throws, or is canceled; in those cases the tracked changes are discarded. Queries are passed through unchanged.</para>
///     <para xml:lang="ja">成功したCommandの変更を<see cref="IUnitOfWork"/>経由で保存するPipeline Behavior。Handlerが失敗の<see cref="Result"/>を返した場合、例外を送出した場合、キャンセルされた場合は保存せず、追跡中の変更を破棄する。Queryはそのまま通過させる。</para>
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
            if (response is not Result result)
            {
                throw new InvalidOperationException(ExceptionMessages.InvalidOperationException_CommandResponseMustBeResult);
            }

            if (result.IsFailure)
            {
                unitOfWork.DiscardChanges();
                return response;
            }

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
