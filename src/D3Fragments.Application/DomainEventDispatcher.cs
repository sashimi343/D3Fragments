using System.Collections.Concurrent;
using System.Reflection;

namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">The default <see cref="IDomainEventDispatcher"/> implementation that resolves <see cref="IDomainEventHandler{TEvent}"/> instances from the service provider and invokes them.</para>
///     <para xml:lang="ja">サービスプロバイダーから<see cref="IDomainEventHandler{TEvent}"/>を解決して呼び出す、既定の<see cref="IDomainEventDispatcher"/>実装。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Only handlers whose type argument matches the runtime type of the event are invoked. Handlers for base types or interfaces of the event are not invoked. Handlers run sequentially in registration order. When a handler throws an exception, the remaining handlers are not invoked and the exception propagates to the caller. When no handler is registered, this class does nothing.</para>
///     <para xml:lang="ja">イベントの実行時型と型引数が一致するハンドラーのみを呼び出す。イベントの基底型やインターフェースを対象とするハンドラーは呼び出さない。ハンドラーは登録順に逐次実行する。ハンドラーが例外をスローした場合、残りのハンドラーは呼び出さず、例外を呼び出し元へ伝播する。ハンドラーが登録されていない場合は何もしない。</para>
/// </remarks>
/// <param name="serviceProvider">
///     <para xml:lang="en">The service provider that resolves the handlers.</para>
///     <para xml:lang="ja">ハンドラーを解決するサービスプロバイダー。</para>
/// </param>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDomainEvent, CancellationToken, Task>> Invokers = new();

    private static readonly MethodInfo InvokeHandlersMethod =
        typeof(DomainEventDispatcher).GetMethod(nameof(InvokeHandlersAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="domainEvent"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // 実行時型ごとにジェネリックメソッドを閉じたデリゲートをキャッシュし、呼び出しごとのリフレクションを避ける
        var invoker = Invokers.GetOrAdd(
            domainEvent.GetType(),
            static eventType => InvokeHandlersMethod.MakeGenericMethod(eventType)
                .CreateDelegate<Func<IServiceProvider, IDomainEvent, CancellationToken, Task>>());
        return invoker(serviceProvider, domainEvent, cancellationToken);
    }

    private static async Task InvokeHandlersAsync<TEvent>(IServiceProvider serviceProvider, IDomainEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        var handlers = (IEnumerable<IDomainEventHandler<TEvent>>?)serviceProvider.GetService(typeof(IEnumerable<IDomainEventHandler<TEvent>>)) ?? [];
        foreach (var handler in handlers)
        {
            await handler.HandleAsync((TEvent)domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }
}
