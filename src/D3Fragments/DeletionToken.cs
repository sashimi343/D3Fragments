namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents proof that an aggregate has passed its own deletion rules. Instances can only be issued by the aggregate itself through <see cref="AggregateRoot{TId}.AllowDeletion{T}(T)"/>.</para>
///     <para xml:lang="ja">集約が自身の削除ルールを通過したことを表す証明。インスタンスは集約自身が<see cref="AggregateRoot{TId}.AllowDeletion{T}(T)"/>を通じてのみ発行できる。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Deletion APIs such as repositories accept this token instead of an ID, so that deletion cannot be performed without going through the domain logic.</para>
///     <para xml:lang="ja">リポジトリ等の削除APIはIDの代わりにこのトークンを受け取る。これにより、ドメインロジックを経由しない削除を防ぐ。</para>
/// </remarks>
/// <typeparam name="T">
///     <para xml:lang="en">The type of the aggregate to delete.</para>
///     <para xml:lang="ja">削除対象の集約の型。</para>
/// </typeparam>
public sealed class DeletionToken<T>
    where T : class
{
    internal DeletionToken(T entity) => Entity = entity;

    /// <summary>
    ///     <para xml:lang="en">Gets the aggregate that is allowed to be deleted.</para>
    ///     <para xml:lang="ja">削除が許可された集約を取得する。</para>
    /// </summary>
    /// <value>
    ///     <para xml:lang="en">The aggregate instance that issued this token.</para>
    ///     <para xml:lang="ja">このトークンを発行した集約インスタンス。</para>
    /// </value>
    public T Entity { get; }
}
