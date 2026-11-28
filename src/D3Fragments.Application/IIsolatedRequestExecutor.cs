namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Executes a request of type <typeparamref name="TRequest"/> in a new dependency injection scope, separately from the caller's scope.</para>
///     <para xml:lang="ja">型<typeparamref name="TRequest"/>のリクエストを、呼び出し元とは別の新しいDIスコープで実行するインターフェース。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">The request uses its own <see cref="IUnitOfWork"/> and is saved and committed independently of the caller. Changes are kept even if the caller fails afterwards, and a failure of the request does not discard the caller's changes. Use it from a command handler or a before-commit domain event handler to run work that must be committed independently, such as a preprocessing step that must be committed before the main processing, or a record that must remain even if the main processing fails.</para>
///     <para xml:lang="ja">リクエストは呼び出し元とは別の<see cref="IUnitOfWork"/>を使い、呼び出し元とは独立して保存・Commitされる。呼び出し元が後で失敗しても変更は残り、リクエストが失敗しても呼び出し元の変更は破棄されない。本体より前に確定させたい前処理や、本体が失敗しても残したい記録など、独立して確定させたい処理を、CommandのHandlerやCommit前のドメインイベントハンドラから実行するときに使う。</para>
///     <para xml:lang="en">The request cannot see rows that the caller has not committed yet. Updating a row that the caller's transaction has locked makes the request wait indefinitely.</para>
///     <para xml:lang="ja">リクエストからは、呼び出し元がまだCommitしていない行は見えない。呼び出し元のトランザクションがロックしている行を更新すると、リクエストは待ち続ける。</para>
/// </remarks>
/// <typeparam name="TRequest">
///     <para xml:lang="en">The type of the request.</para>
///     <para xml:lang="ja">リクエストの型。</para>
/// </typeparam>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
public interface IIsolatedRequestExecutor<in TRequest, TResponse> : IRequestExecutor<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;
