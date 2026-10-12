using System.Collections.Concurrent;
using System.Reflection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">The default <see cref="IDomainEventDispatcher"/> implementation that resolves <see cref="IBeforeCommitDomainEventHandler{TEvent}"/> and <see cref="IAfterCommitDomainEventHandler{TEvent}"/> instances from the service provider and invokes them.</para>
///     <para xml:lang="ja">サービスプロバイダーから<see cref="IBeforeCommitDomainEventHandler{TEvent}"/>および<see cref="IAfterCommitDomainEventHandler{TEvent}"/>を解決して呼び出す、既定の<see cref="IDomainEventDispatcher"/>実装。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Only handlers whose type argument matches the runtime type of the event are invoked. Handlers for base types or interfaces of the event are not invoked. Handlers run sequentially in registration order. When an <see cref="IBeforeCommitDomainEventHandler{TEvent}"/> throws an exception, the remaining handlers are not invoked and the exception propagates to the caller. When an <see cref="IAfterCommitDomainEventHandler{TEvent}"/> throws an exception, the exception is logged at the error level with <see cref="D3FragmentsApplicationOptions.AfterCommitHandlerFailedEventId"/>, and the remaining handlers are still invoked. When no handler is registered, this class does nothing.</para>
///     <para xml:lang="ja">イベントの実行時型と型引数が一致するハンドラーのみを呼び出す。イベントの基底型やインターフェースを対象とするハンドラーは呼び出さない。ハンドラーは登録順に逐次実行する。<see cref="IBeforeCommitDomainEventHandler{TEvent}"/>が例外をスローした場合、残りのハンドラーは呼び出さず、例外を呼び出し元へ伝播する。<see cref="IAfterCommitDomainEventHandler{TEvent}"/>が例外をスローした場合、<see cref="D3FragmentsApplicationOptions.AfterCommitHandlerFailedEventId"/>のエラーログを出力し、残りのハンドラーは引き続き呼び出す。ハンドラーが登録されていない場合は何もしない。</para>
/// </remarks>
/// <param name="serviceProvider">
///     <para xml:lang="en">The service provider that resolves the handlers.</para>
///     <para xml:lang="ja">ハンドラーを解決するサービスプロバイダー。</para>
/// </param>
/// <param name="logger">
///     <para xml:lang="en">The logger that records exceptions thrown by <see cref="IAfterCommitDomainEventHandler{TEvent}"/>.</para>
///     <para xml:lang="ja"><see cref="IAfterCommitDomainEventHandler{TEvent}"/>がスローした例外を記録するロガー。</para>
/// </param>
/// <param name="options">
///     <para xml:lang="en">The application layer options.</para>
///     <para xml:lang="ja">アプリケーション層のオプション。</para>
/// </param>
public sealed class DomainEventDispatcher(
    IServiceProvider serviceProvider,
    ILogger<DomainEventDispatcher> logger,
    IOptions<D3FragmentsApplicationOptions> options)
    : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Invokers> InvokersByEventType = new();

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="domainEvent"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public Task DispatchBeforeCommitAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return GetInvokers(domainEvent).BeforeCommit(this, domainEvent, cancellationToken);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="domainEvent"/> is <see langword="null"/>.</para>
    ///     <para xml:lang="ja"><paramref name="domainEvent"/>が<see langword="null"/>の場合にスローされる。</para>
    /// </exception>
    public Task DispatchAfterCommitAsync(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        return GetInvokers(domainEvent).AfterCommit(this, domainEvent);
    }

    // 実行時型ごとにジェネリックメソッドを閉じたデリゲートをキャッシュし、呼び出しごとのリフレクションを避ける
    private static Invokers GetInvokers(IDomainEvent domainEvent) =>
        InvokersByEventType.GetOrAdd(domainEvent.GetType(), static eventType => new Invokers(
            CreateInvoker<Func<DomainEventDispatcher, IDomainEvent, CancellationToken, Task>>(nameof(InvokeBeforeCommitHandlersAsync), eventType),
            CreateInvoker<Func<DomainEventDispatcher, IDomainEvent, Task>>(nameof(InvokeAfterCommitHandlersAsync), eventType)));

    private static TDelegate CreateInvoker<TDelegate>(string methodName, Type eventType)
        where TDelegate : Delegate =>
        typeof(DomainEventDispatcher).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(eventType)
            .CreateDelegate<TDelegate>();

    private async Task InvokeBeforeCommitHandlersAsync<TEvent>(IDomainEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        foreach (var handler in ResolveHandlers<IBeforeCommitDomainEventHandler<TEvent>>())
        {
            await handler.HandleAsync((TEvent)domainEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task InvokeAfterCommitHandlersAsync<TEvent>(IDomainEvent domainEvent)
        where TEvent : IDomainEvent
    {
        foreach (var handler in ResolveHandlers<IAfterCommitDomainEventHandler<TEvent>>())
        {
            try
            {
                await handler.HandleAsync((TEvent)domainEvent, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 業務データは確定済みのため、呼び出し元へ伝播せずログに出力して後続のハンドラーを続行する
                // イベントIDをオプションで変更可能にするため、LoggerMessageのソースジェネレーター（定数のイベントIDのみ対応）は使わない
                logger.LogError(
                    options.Value.AfterCommitHandlerFailedEventId,
                    ex,
                    "After-commit domain event handler {HandlerType} failed to handle {EventType}.",
                    handler.GetType().FullName,
                    typeof(TEvent).FullName);
            }
        }
    }

    private IEnumerable<THandler> ResolveHandlers<THandler>() =>
        (IEnumerable<THandler>?)serviceProvider.GetService(typeof(IEnumerable<THandler>)) ?? [];

    private sealed record Invokers(
        Func<DomainEventDispatcher, IDomainEvent, CancellationToken, Task> BeforeCommit,
        Func<DomainEventDispatcher, IDomainEvent, Task> AfterCommit);
}
