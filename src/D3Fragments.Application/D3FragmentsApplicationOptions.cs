using Microsoft.Extensions.Logging;

namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Provides options for the application layer features registered by <see cref="D3FragmentsApplicationServiceCollectionExtensions.AddD3FragmentsApplication"/>.</para>
///     <para xml:lang="ja"><see cref="D3FragmentsApplicationServiceCollectionExtensions.AddD3FragmentsApplication"/>で登録するアプリケーション層の機能のオプションを提供する。</para>
/// </summary>
public sealed class D3FragmentsApplicationOptions
{
    /// <summary>
    ///     <para xml:lang="en">Gets or sets the event ID of the error log written when an <see cref="IAfterCommitDomainEventHandler{TEvent}"/> throws an exception.</para>
    ///     <para xml:lang="ja"><see cref="IAfterCommitDomainEventHandler{TEvent}"/>が例外をスローした場合に出力するエラーログのイベントIDを取得または設定する。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en">The event ID. The default is <c>1001</c> (<c>AfterCommitHandlerFailed</c>).</para>
    ///     <para xml:lang="ja">イベントID。既定値は<c>1001</c>（<c>AfterCommitHandlerFailed</c>）。</para>
    /// </value>
    public EventId AfterCommitHandlerFailedEventId { get; set; } = new(1001, "AfterCommitHandlerFailed");

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the event ID of the error log written when unsaved changes remain after the <see cref="IAfterCommitDomainEventHandler{TEvent}"/> instances have been invoked, which means that a handler modified entities directly. The changes are discarded.</para>
    ///     <para xml:lang="ja"><see cref="IAfterCommitDomainEventHandler{TEvent}"/>の呼び出し後に未保存の変更が残っていた（ハンドラーがエンティティを直接変更した）場合に出力するエラーログのイベントIDを取得または設定する。変更は破棄される。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en">The event ID. The default is <c>1002</c> (<c>AfterCommitChangesDiscarded</c>).</para>
    ///     <para xml:lang="ja">イベントID。既定値は<c>1002</c>（<c>AfterCommitChangesDiscarded</c>）。</para>
    /// </value>
    public EventId AfterCommitChangesDiscardedEventId { get; set; } = new(1002, "AfterCommitChangesDiscarded");
}
