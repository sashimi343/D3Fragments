namespace D3Fragments.Application;

/// <summary>
///     <para xml:lang="en">Marker interface for a request that returns <typeparamref name="TResponse"/>. Common to both commands and queries.</para>
///     <para xml:lang="ja">Command/Queryに共通する、<typeparamref name="TResponse"/>を返すリクエストのマーカーインターフェース。</para>
/// </summary>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
public interface IRequest<TResponse>
{
}

/// <summary>
///     <para xml:lang="en">Marker interface identifying a request as a command (a request that changes state).</para>
///     <para xml:lang="ja">リクエストがCommand（状態を変更するリクエスト）であることを識別するマーカーインターフェース。</para>
/// </summary>
public interface ICommand
{
}

/// <summary>
///     <para xml:lang="en">A command that returns <typeparamref name="TResponse"/> on success. Success/failure is not expressed via the response's type; a handler indicates failure by throwing (typically <see cref="DomainException"/>), and pipeline behaviors (<see cref="UnitOfWorkBehavior{TRequest, TResponse}"/>, <see cref="TransactionBehavior{TRequest, TResponse}"/>) decide whether to commit based solely on whether an exception propagated.</para>
///     <para xml:lang="ja">成功時に<typeparamref name="TResponse"/>を返すCommand。成功・失敗は応答の型では表現しない。Handlerは失敗を例外（通常は<see cref="DomainException"/>）のスローで表現し、Pipeline Behavior（<see cref="UnitOfWorkBehavior{TRequest, TResponse}"/>、<see cref="TransactionBehavior{TRequest, TResponse}"/>）は例外が伝播したかどうかだけでCommit/Rollbackを判断する。</para>
/// </summary>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response produced on success.</para>
///     <para xml:lang="ja">成功時に生成される応答の型。</para>
/// </typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>, ICommand
{
}

/// <summary>
///     <para xml:lang="en">Marker interface for a read-only request that returns <typeparamref name="TResponse"/>.</para>
///     <para xml:lang="ja">読み取り専用のリクエストであることを識別する、<typeparamref name="TResponse"/>を返すマーカーインターフェース。</para>
/// </summary>
/// <typeparam name="TResponse">
///     <para xml:lang="en">The type of the response.</para>
///     <para xml:lang="ja">応答の型。</para>
/// </typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}

/// <summary>
///     <para xml:lang="en">Marker interface for a command that requires an explicit database transaction around handler execution (e.g. to hold a row lock before the handler runs). Most commands do not need this; a single call to <c>SaveChangesAsync</c> after a successful handler is usually sufficient.</para>
///     <para xml:lang="ja">Handler実行の前後を明示的なDBトランザクションで囲む必要があるCommand（行ロック等のため）を識別するマーカーインターフェース。多くのCommandには不要で、Handler成功後の1回の<c>SaveChangesAsync</c>で十分な場合が多い。</para>
/// </summary>
public interface ITransactionalCommand : ICommand
{
}
