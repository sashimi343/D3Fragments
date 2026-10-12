namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Handles a domain event of type <typeparamref name="TEvent"/> before the commit, within the same transaction as the business data.</para>
///     <para xml:lang="ja">指定した型のドメインイベントを、業務データと同一のトランザクション内でCommit前に処理するハンドラーのインターフェース。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Use this handler for processing that updates the database. Changes made by the handler are saved together with the business data, and are rolled back together when the handler or the save fails. An exception thrown by the handler propagates to the caller of the command. Do not perform external I/O (sending e-mails, calling external APIs, and so on) in this handler, because it is not undone when the transaction is rolled back; use <see cref="IAfterCommitDomainEventHandler{TEvent}"/> instead.</para>
///     <para xml:lang="ja">DB更新を伴う処理に使用する。ハンドラーによる変更は業務データとまとめて保存され、ハンドラーまたは保存が失敗した場合はまとめてロールバックされる。ハンドラーがスローした例外はCommandの呼び出し元へ伝播する。トランザクションがロールバックされても取り消せないため、外部I/O（メール送信、外部APIの呼び出し等）はこのハンドラーで行わず、<see cref="IAfterCommitDomainEventHandler{TEvent}"/>を使用すること。</para>
/// </remarks>
/// <typeparam name="TEvent">
///     <para xml:lang="en">The type of the domain event to handle.</para>
///     <para xml:lang="ja">処理対象のドメインイベントの型。</para>
/// </typeparam>
public interface IBeforeCommitDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    /// <summary>
    ///     <para xml:lang="en">Handles a domain event asynchronously.</para>
    ///     <para xml:lang="ja">ドメインイベントを非同期的に処理する。</para>
    /// </summary>
    /// <param name="domainEvent">
    ///     <para xml:lang="en">The domain event to handle.</para>
    ///     <para xml:lang="ja">処理対象のドメインイベント。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
