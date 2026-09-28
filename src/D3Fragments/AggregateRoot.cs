namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Base class for aggregate roots. Only aggregate roots can raise domain events. The ID type is free (<typeparamref name="TId"/>).</para>
///     <para xml:lang="ja">集約ルートの基底クラス。ドメインイベントを発行できるのは集約ルートのみ。ID型は<typeparamref name="TId"/>で自由化されている。</para>
/// </summary>
/// <typeparam name="TId">
///     <para xml:lang="en">The type of the entity's ID.</para>
///     <para xml:lang="ja">エンティティのID型。</para>
/// </typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc cref="Entity{TId}(TId, DateTimeOffset)" />
    protected AggregateRoot(TId id, DateTimeOffset createdAt) : base(id, createdAt)
    {
    }

    /// <summary>
    ///     <para xml:lang="en">Adds a domain event to this aggregate's pending queue.</para>
    ///     <para xml:lang="ja">ドメインイベントをこの集約の発行キューに追加する。</para>
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
}

/// <summary>
///     <para xml:lang="en">Shortcut base class for the most common case: an aggregate root with an auto-assigned <see cref="UuidV7"/> ID. Inherit <see cref="AggregateRoot{TId}"/> directly when a different ID type is needed.</para>
///     <para xml:lang="ja">UUIDv7自動採番の集約ルート向けショートカット基底クラス。ID型を自由化したい場合は<see cref="AggregateRoot{TId}"/>を直接継承すること。</para>
/// </summary>
public abstract class AggregateRoot : AggregateRoot<UuidV7>
{
    /// <inheritdoc cref="Entity(DateTimeOffset)" />
    protected AggregateRoot(DateTimeOffset createdAt) : base(UuidV7.NewUuidV7(), createdAt)
    {
    }
}
