namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Base class for entities that track their creation and last update timestamps. The ID type is free (<typeparamref name="TId"/>).</para>
///     <para xml:lang="ja">作成日時・更新日時を追跡するエンティティの基底クラス。ID型は<typeparamref name="TId"/>で自由化されている。</para>
/// </summary>
/// <typeparam name="TId">
///     <para xml:lang="en">The type of the entity's ID.</para>
///     <para xml:lang="ja">エンティティのID型。</para>
/// </typeparam>
public abstract class Entity<TId> : EntityBase<TId>, IUpdateTrackable
    where TId : notnull
{
    /// <summary>
    ///     <para xml:lang="en">Gets the date and time this entity was created.</para>
    ///     <para xml:lang="ja">このエンティティが作成された日時を取得する。</para>
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">Gets the date and time this entity was last updated. Initialized to <see cref="CreatedAt"/>.</para>
    ///     <para xml:lang="ja">このエンティティが最後に更新された日時を取得する。初期値は<see cref="CreatedAt"/>。</para>
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="Entity{TId}"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="Entity{TId}"/> を初期化する。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The ID of the entity. Must not be the default value of <typeparamref name="TId"/>.</para>
    ///     <para xml:lang="ja">エンティティのID。<typeparamref name="TId"/>の既定値であってはならない。</para>
    /// </param>
    /// <param name="createdAt">
    ///     <para xml:lang="en">The creation timestamp. The caller (e.g. a factory method) supplies this explicitly via a <see cref="TimeProvider"/> so that the entity itself does not depend on the wall clock.</para>
    ///     <para xml:lang="ja">作成日時。エンティティ自体が実時計に依存しないよう、呼び出し側（ファクトリメソッド等）が<see cref="TimeProvider"/>経由で明示的に渡す。</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="id"/> is the default value of <typeparamref name="TId"/>.</para>
    ///     <para xml:lang="ja"><paramref name="id"/>が<typeparamref name="TId"/>の既定値の場合にスローされる。</para>
    /// </exception>
    protected Entity(TId id, DateTimeOffset createdAt) : base(id)
    {
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <inheritdoc />
    void IUpdateTrackable.UpdateTimestamp(DateTimeOffset now) => UpdatedAt = now;
}

/// <summary>
///     <para xml:lang="en">Shortcut base class for the most common case: an entity with an auto-assigned <see cref="UuidV7"/> ID that tracks timestamps. Inherit <see cref="Entity{TId}"/> directly when a different ID type is needed.</para>
///     <para xml:lang="ja">UUIDv7自動採番・日時追跡ありエンティティ向けのショートカット基底クラス。ID型を自由化したい場合は<see cref="Entity{TId}"/>を直接継承すること。</para>
/// </summary>
public abstract class Entity : Entity<UuidV7>
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="Entity"/> with a newly generated <see cref="UuidV7"/> ID.</para>
    ///     <para xml:lang="ja">新規採番した<see cref="UuidV7"/>をIDとして、新しい <see cref="Entity"/> を初期化する。</para>
    /// </summary>
    /// <param name="createdAt">
    ///     <para xml:lang="en">The creation timestamp.</para>
    ///     <para xml:lang="ja">作成日時。</para>
    /// </param>
    protected Entity(DateTimeOffset createdAt) : base(UuidV7.NewUuidV7(), createdAt)
    {
    }
}
