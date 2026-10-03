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

    /// <inheritdoc cref="Entity{TId}(TId)" />
    protected AggregateRoot(TId id) : base(id)
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

    /// <summary>
    ///     <para xml:lang="en">Issues a <see cref="DeletionToken{T}"/> for this aggregate. Call this from the aggregate's own deletion method after its deletion rules have passed.</para>
    ///     <para xml:lang="ja">この集約の<see cref="DeletionToken{T}"/>を発行する。集約自身の削除メソッド内で、削除ルールを通過した後に呼び出す。</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the aggregate. Inferred from <paramref name="self"/> when <see langword="this"/> is passed.</para>
    ///     <para xml:lang="ja">集約の型。<paramref name="self"/>に<see langword="this"/>を渡すと推論される。</para>
    /// </typeparam>
    /// <param name="self">
    ///     <para xml:lang="en">This aggregate instance. Pass <see langword="this"/>.</para>
    ///     <para xml:lang="ja">この集約インスタンス。<see langword="this"/>を渡す。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A token that allows this aggregate to be deleted.</para>
    ///     <para xml:lang="ja">この集約の削除を許可するトークン。</para>
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     <para xml:lang="en">Thrown when <paramref name="self"/> is not this instance.</para>
    ///     <para xml:lang="ja"><paramref name="self"/>がこのインスタンス自身でない場合にスローされる。</para>
    /// </exception>
    protected DeletionToken<T> AllowDeletion<T>(T self)
        where T : AggregateRoot<TId>
    {
        if (!ReferenceEquals(self, this))
        {
            throw new ArgumentException(ExceptionMessages.ArgumentException_DeletionTargetMustBeSelf, nameof(self));
        }

        return new DeletionToken<T>(self);
    }

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
    /// <inheritdoc cref="Entity()" />
    protected AggregateRoot() : base(UuidV7.NewUuidV7())
    {
    }
}
