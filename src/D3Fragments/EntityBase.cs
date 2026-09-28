namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Root base class for all entities: holds an ID and provides identity-based equality. The ID type is free (<typeparamref name="TId"/>). Inherit <see cref="Entity{TId}"/> when timestamps are needed, or <see cref="AggregateRoot{TId}"/> for an aggregate root.</para>
///     <para xml:lang="ja">全エンティティの共通土台。IDを持ち、IDによる同一性判定を提供する。ID型は<typeparamref name="TId"/>で自由化されている。日時追跡が必要な場合は<see cref="Entity{TId}"/>を、集約ルートの場合は<see cref="AggregateRoot{TId}"/>を継承する。</para>
/// </summary>
/// <typeparam name="TId">
///     <para xml:lang="en">The type of the entity's ID.</para>
///     <para xml:lang="ja">エンティティのID型。</para>
/// </typeparam>
public abstract class EntityBase<TId> : IEquatable<EntityBase<TId>>
    where TId : notnull
{
    /// <summary>
    ///     <para xml:lang="en">Gets the ID of this entity.</para>
    ///     <para xml:lang="ja">このエンティティのIDを取得する。</para>
    /// </summary>
    public TId Id { get; protected set; }

    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="EntityBase{TId}"/>.</para>
    ///     <para xml:lang="ja">新しい <see cref="EntityBase{TId}"/> を初期化する。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The ID of the entity. Must not be the default value of <typeparamref name="TId"/>.</para>
    ///     <para xml:lang="ja">エンティティのID。<typeparamref name="TId"/>の既定値であってはならない。</para>
    /// </param>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="id"/> is the default value of <typeparamref name="TId"/>.</para>
    ///     <para xml:lang="ja"><paramref name="id"/>が<typeparamref name="TId"/>の既定値の場合にスローされる。</para>
    /// </exception>
    protected EntityBase(TId id)
    {
        if (EqualityComparer<TId>.Default.Equals(id, default!))
        {
            throw new ArgumentException(ExceptionMessages.ArgumentException_EntityIdCannotBeDefault, nameof(id));
        }

        Id = id;
    }

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
///     <para xml:lang="en">Shortcut base class with an auto-assigned <see cref="UuidV7"/> ID. Inherit <see cref="EntityBase{TId}"/> directly when a different ID type is needed.</para>
///     <para xml:lang="ja">UUIDv7自動採番のショートカット基底クラス。ID型を自由化したい場合は<see cref="EntityBase{TId}"/>を直接継承すること。</para>
/// </summary>
public abstract class EntityBase : EntityBase<UuidV7>
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new <see cref="EntityBase"/> with a newly generated <see cref="UuidV7"/> ID.</para>
    ///     <para xml:lang="ja">新規採番した<see cref="UuidV7"/>をIDとして、新しい <see cref="EntityBase"/> を初期化する。</para>
    /// </summary>
    protected EntityBase() : base(UuidV7.NewUuidV7())
    {
    }
}
