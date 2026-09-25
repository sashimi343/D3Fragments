namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Base class for all entities. The ID type is free (<typeparamref name="TId"/>). By itself this class represents an append-only (insert-only) entity; inherit <see cref="MutableEntityBase{TId}"/> instead when the entity needs to be updated.</para>
///     <para xml:lang="ja">全エンティティの共通土台。ID型は<typeparamref name="TId"/>で自由化されている。これ自体は追記のみ（Insert-only）の性質を表し、更新が必要な場合は<see cref="MutableEntityBase{TId}"/>を継承する。</para>
/// </summary>
/// <typeparam name="TId">
///     <para xml:lang="en">The type of the entity's ID.</para>
///     <para xml:lang="ja">エンティティのID型。</para>
/// </typeparam>
public abstract class EntityBase<TId> : IEquatable<EntityBase<TId>>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    ///     <para xml:lang="en">Gets the ID of this entity.</para>
    ///     <para xml:lang="ja">このエンティティのIDを取得する。</para>
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    ///     <para xml:lang="en">Gets the date and time this entity was created.</para>
    ///     <para xml:lang="ja">このエンティティが作成された日時を取得する。</para>
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="EntityBase{TId}"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="EntityBase{TId}"/> を初期化する。</para>
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
    protected EntityBase(TId id, DateTimeOffset createdAt)
    {
        if (EqualityComparer<TId>.Default.Equals(id, default!))
        {
            throw new ArgumentException(ExceptionMessages.ArgumentException_EntityIdCannotBeDefault, nameof(id));
        }

        Id = id;
        CreatedAt = createdAt;
    }

    /// <summary>
    ///     <para xml:lang="en">Adds a domain event to this entity's pending queue.</para>
    ///     <para xml:lang="ja">ドメインイベントをこのエンティティの発行キューに追加する。</para>
    /// </summary>
    /// <param name="domainEvent">
    ///     <para xml:lang="en">The domain event to add.</para>
    ///     <para xml:lang="ja">追加するドメインイベント。</para>
    /// </param>
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    IReadOnlyList<IDomainEvent> IHasDomainEvents.DomainEvents => _domainEvents;

    /// <inheritdoc />
    void IHasDomainEvents.ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    ///     <para xml:lang="en">Determines whether this instance and <paramref name="other"/> represent the same entity by comparing their runtime type and ID. Because the constructor rejects a default ID, an entity with an "unassigned" ID can never exist, so two distinct entities can never be incorrectly considered equal through a shared default ID.</para>
    ///     <para xml:lang="ja">実行時の型とIDの一致により、このインスタンスと<paramref name="other"/>が同一エンティティかどうかを判定する。コンストラクタで既定値のIDを拒否しているため「未確定IDを持つエンティティ」は存在しえず、既定値IDの一致により誤って等価と判定されることは構造的に発生しない。</para>
    /// </summary>
    public bool Equals(EntityBase<TId>? other)
    {
        if (other is null)
        {
            return false;
        }
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (GetType() != other.GetType())
        {
            return false;
        }
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as EntityBase<TId>);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    /// <summary>
    ///     <para xml:lang="en">Determines whether two entities are equal.</para>
    ///     <para xml:lang="ja">2つのエンティティが等しいかどうかを判定する。</para>
    /// </summary>
    public static bool operator ==(EntityBase<TId>? left, EntityBase<TId>? right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>
    ///     <para xml:lang="en">Determines whether two entities are not equal.</para>
    ///     <para xml:lang="ja">2つのエンティティが等しくないかどうかを判定する。</para>
    /// </summary>
    public static bool operator !=(EntityBase<TId>? left, EntityBase<TId>? right) => !(left == right);
}

/// <summary>
///     <para xml:lang="en">Base class for entities that are updated and therefore need to track their <see cref="UpdatedAt"/> timestamp. The ID type is free (<typeparamref name="TId"/>).</para>
///     <para xml:lang="ja">更新される（<see cref="UpdatedAt"/>追跡が必要な）エンティティの基底クラス。ID型は<typeparamref name="TId"/>で自由化されている。</para>
/// </summary>
/// <typeparam name="TId">
///     <para xml:lang="en">The type of the entity's ID.</para>
///     <para xml:lang="ja">エンティティのID型。</para>
/// </typeparam>
public abstract class MutableEntityBase<TId> : EntityBase<TId>, IUpdateTrackable
    where TId : notnull
{
    /// <summary>
    ///     <para xml:lang="en">Gets the date and time this entity was last updated.</para>
    ///     <para xml:lang="ja">このエンティティが最後に更新された日時を取得する。</para>
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <inheritdoc cref="EntityBase{TId}(TId, DateTimeOffset)" />
    protected MutableEntityBase(TId id, DateTimeOffset createdAt) : base(id, createdAt)
    {
    }

    /// <inheritdoc />
    void IUpdateTrackable.UpdateTimestamp(DateTimeOffset now) => UpdatedAt = now;
}

/// <summary>
///     <para xml:lang="en">Shortcut base class for the most common case: an append-only (insert-only) entity with an auto-assigned <see cref="UuidV7"/> ID. Inherit <see cref="EntityBase{TId}"/> directly when a different ID type is needed.</para>
///     <para xml:lang="ja">UUIDv7自動採番・追記専用(Insert-only)エンティティ向けのショートカット基底クラス。最も一般的なケースで使用する。ID型を自由化したい場合は<see cref="EntityBase{TId}"/>を直接継承すること。</para>
/// </summary>
public abstract class EntityBase : EntityBase<UuidV7>
{
    /// <inheritdoc cref="EntityBase{TId}(TId, DateTimeOffset)" />
    protected EntityBase(DateTimeOffset createdAt) : base(UuidV7.NewUuidV7(), createdAt)
    {
    }
}

/// <summary>
///     <para xml:lang="en">Shortcut base class for the most common case: an entity with an auto-assigned <see cref="UuidV7"/> ID that tracks updates. Inherit <see cref="MutableEntityBase{TId}"/> directly when a different ID type is needed.</para>
///     <para xml:lang="ja">UUIDv7自動採番・更新追跡ありエンティティ向けのショートカット基底クラス。最も一般的なケースで使用する。ID型を自由化したい場合は<see cref="MutableEntityBase{TId}"/>を直接継承すること。</para>
/// </summary>
public abstract class MutableEntityBase : MutableEntityBase<UuidV7>
{
    /// <inheritdoc cref="EntityBase{TId}(TId, DateTimeOffset)" />
    protected MutableEntityBase(DateTimeOffset createdAt) : base(UuidV7.NewUuidV7(), createdAt)
    {
    }
}
