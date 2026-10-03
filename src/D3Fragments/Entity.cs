namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Base class for entities that track their creation and last update timestamps and a version number for optimistic concurrency control. The ID type is free (<typeparamref name="TId"/>).</para>
///     <para xml:lang="ja">作成日時・更新日時と、楽観的同時実行制御用のバージョン番号を追跡するエンティティの基底クラス。ID型は<typeparamref name="TId"/>で自由化されている。</para>
/// </summary>
/// <typeparam name="TId">
///     <para xml:lang="en">The type of the entity's ID.</para>
///     <para xml:lang="ja">エンティティのID型。</para>
/// </typeparam>
public abstract class Entity<TId> : EntityBase<TId>, IUpdateTrackable
    where TId : notnull
{
    /// <summary>
    ///     <para xml:lang="en">Gets the system time (UTC) when this entity was created. This is an audit field recorded automatically; keep timestamps that have domain meaning in separate properties.</para>
    ///     <para xml:lang="ja">このエンティティが作成されたシステム日時（UTC）を取得する。自動記録される監査項目であり、ドメイン上意味のある日時は別のプロパティで保持すること。</para>
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">Gets the system time (UTC) when this entity was last updated. Initialized to <see cref="CreatedAt"/>. This is an audit field recorded automatically.</para>
    ///     <para xml:lang="ja">このエンティティが最後に更新されたシステム日時（UTC）を取得する。初期値は<see cref="CreatedAt"/>。自動記録される監査項目である。</para>
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">Gets the version number used for optimistic concurrency control. Starts at 0 and is incremented on every update.</para>
    ///     <para xml:lang="ja">楽観的同時実行制御に使用するバージョン番号を取得する。0から始まり、更新のたびに1加算される。</para>
    /// </summary>
    public long Version { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="Entity{TId}"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="Entity{TId}"/> を初期化する。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The ID of the entity. Must not be the default value of <typeparamref name="TId"/>.</para>
    ///     <para xml:lang="ja">エンティティのID。<typeparamref name="TId"/>の既定値であってはならない。</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="id"/> is the default value of <typeparamref name="TId"/>.</para>
    ///     <para xml:lang="ja"><paramref name="id"/>が<typeparamref name="TId"/>の既定値の場合にスローされる。</para>
    /// </exception>
    protected Entity(TId id) : base(id)
    {
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    /// <inheritdoc />
    void IUpdateTrackable.MarkUpdated()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        Version++;
    }
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
    protected Entity() : base(UuidV7.NewUuidV7())
    {
    }
}
