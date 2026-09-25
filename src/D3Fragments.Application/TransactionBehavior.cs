using D3Fragments;

namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Wraps handler execution in an explicit database transaction for commands marked with <see cref="ITransactionalCommand"/> (e.g. to hold a row lock before the handler runs). Other requests are passed through unchanged. Runs outside <see cref="UnitOfWorkBehavior{TRequest, TResponse}"/> so the transaction covers that behavior's <c>SaveChangesAsync</c> call as well. Commit/rollback is decided solely by whether the pipeline threw, not by inspecting the response.</para>
///     <para xml:lang="ja"><see cref="ITransactionalCommand"/>が指定されたCommand（行ロック等のためHandler実行前から明示的なトランザクションが必要な場合）について、Handler実行を明示的なDBトランザクションで囲むPipeline Behavior。それ以外のリクエストはそのまま通過させる。<see cref="UnitOfWorkBehavior{TRequest, TResponse}"/>の外側で動作し、その<c>SaveChangesAsync</c>呼び出しもトランザクションに含める。Commit/Rollbackは応答の内容を見ず、Pipelineが例外を投げたかどうかだけで決まる。</para>
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public int Order => 200;

    /// <inheritdoc />
    public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) =>
        request is ITransactionalCommand
            ? unitOfWork.ExecuteInTransactionAsync(ct => next(ct), cancellationToken)
            : next(cancellationToken);
}
