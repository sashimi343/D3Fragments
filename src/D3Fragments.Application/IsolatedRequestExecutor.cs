using Microsoft.Extensions.DependencyInjection;

namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">The single open-generic <see cref="IIsolatedRequestExecutor{TRequest, TResponse}"/> implementation. It creates a new scope for each execution and runs the request with the <see cref="IRequestExecutor{TRequest, TResponse}"/> resolved from that scope.</para>
///     <para xml:lang="ja">open genericな唯一の<see cref="IIsolatedRequestExecutor{TRequest, TResponse}"/>実装。実行のたびに新しいスコープを作り、そのスコープから解決した<see cref="IRequestExecutor{TRequest, TResponse}"/>でリクエストを実行する。</para>
/// </summary>
/// <typeparam name="TRequest">
///     <para xml:lang="en">The type of the request.</para>
///     <para xml:lang="ja">リクエストの型。</para>
/// </typeparam>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
/// <param name="scopeFactory">
///     <para xml:lang="en">The factory that creates the scope in which the request is executed.</para>
///     <para xml:lang="ja">リクエストを実行するスコープを作るファクトリ。</para>
/// </param>
public sealed class IsolatedRequestExecutor<TRequest, TResponse>(IServiceScopeFactory scopeFactory)
    : IIsolatedRequestExecutor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var executor = scope.ServiceProvider.GetRequiredService<IRequestExecutor<TRequest, TResponse>>();
            return await executor.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
