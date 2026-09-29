namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Base interface for repositories. A repository is defined per aggregate root; child entities do not have their own repository. Changes to an aggregate obtained from the repository are tracked and persisted by the Unit of Work, so there is no update or save method.</para>
///     <para xml:lang="ja">リポジトリの基底インターフェース。リポジトリは集約ルートごとに定義し、子エンティティ用のリポジトリは作らない。リポジトリから取得した集約への変更はUnit of Workが追跡・永続化するため、更新・保存メソッドは持たない。</para>
/// </summary>
/// <typeparam name="TAggregate">
///     <para xml:lang="en">The type of the aggregate root.</para>
///     <para xml:lang="ja">集約ルートの型。</para>
/// </typeparam>
/// <typeparam name="TId">
///     <para xml:lang="en">The type of the aggregate root's ID.</para>
///     <para xml:lang="ja">集約ルートのID型。</para>
/// </typeparam>
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>
    ///     <para xml:lang="en">Finds an aggregate by its ID.</para>
    ///     <para xml:lang="ja">IDを指定して集約を取得する。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The ID of the aggregate.</para>
    ///     <para xml:lang="ja">集約のID。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The aggregate, or <see langword="null"/> if no aggregate has the specified ID.</para>
    ///     <para xml:lang="ja">集約。指定したIDの集約が存在しない場合は<see langword="null"/>。</para>
    /// </returns>
    Task<TAggregate?> FindAsync(TId id, CancellationToken cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Adds a new aggregate. It is persisted when the Unit of Work saves its changes.</para>
    ///     <para xml:lang="ja">新しい集約を追加する。Unit of Workが変更を保存した時点で永続化される。</para>
    /// </summary>
    /// <param name="aggregate">
    ///     <para xml:lang="en">The aggregate to add.</para>
    ///     <para xml:lang="ja">追加する集約。</para>
    /// </param>
    void Add(TAggregate aggregate);

    /// <summary>
    ///     <para xml:lang="en">Deletes the aggregate whose deletion was allowed by the aggregate itself. It is removed when the Unit of Work saves its changes.</para>
    ///     <para xml:lang="ja">集約自身が削除を許可した集約を削除する。Unit of Workが変更を保存した時点で削除される。</para>
    /// </summary>
    /// <param name="token">
    ///     <para xml:lang="en">The deletion token issued by the aggregate.</para>
    ///     <para xml:lang="ja">集約が発行した削除トークン。</para>
    /// </param>
    void Delete(DeletionToken<TAggregate> token);

    /// <summary>
    ///     <para xml:lang="en">Gets the number of stored aggregates.</para>
    ///     <para xml:lang="ja">永続化されている集約の件数を取得する。</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The number of aggregates.</para>
    ///     <para xml:lang="ja">集約の件数。</para>
    /// </returns>
    Task<int> CountAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Determines whether an aggregate with the specified ID exists, without loading it.</para>
    ///     <para xml:lang="ja">指定したIDの集約が存在するかを、集約を読み込まずに判定する。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The ID of the aggregate.</para>
    ///     <para xml:lang="ja">集約のID。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="ja">キャンセルトークン。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en"><see langword="true"/> if the aggregate exists; otherwise, <see langword="false"/>.</para>
    ///     <para xml:lang="ja">存在する場合は<see langword="true"/>、それ以外の場合は<see langword="false"/>。</para>
    /// </returns>
    Task<bool> ExistsAsync(TId id, CancellationToken cancellationToken);
}

/// <summary>
///     <para xml:lang="en">Base interface for repositories of aggregate roots whose ID is a <see cref="UuidV7"/>. A shortcut for <see cref="IRepository{TAggregate, TId}"/>.</para>
///     <para xml:lang="ja">IDが<see cref="UuidV7"/>の集約ルート用リポジトリの基底インターフェース。<see cref="IRepository{TAggregate, TId}"/>のショートカット。</para>
/// </summary>
/// <typeparam name="TAggregate">
///     <para xml:lang="en">The type of the aggregate root.</para>
///     <para xml:lang="ja">集約ルートの型。</para>
/// </typeparam>
public interface IRepository<TAggregate> : IRepository<TAggregate, UuidV7>
    where TAggregate : AggregateRoot<UuidV7>
{
}
