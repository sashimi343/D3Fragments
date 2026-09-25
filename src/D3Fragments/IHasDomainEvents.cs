namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Implemented by an entity that can raise domain events. This is an infrastructure-facing contract: an entity's events are read by the Unit of Work and cleared only after a successful commit.</para>
///     <para xml:lang="ja">ドメインイベントを発行できるエンティティが実装するインターフェース。Infrastructure向けの契約であり、Unit of WorkがDBのCommit成功後にだけイベントを参照・クリアする想定。</para>
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    ///     <para xml:lang="en">Gets the domain events raised by this entity that have not yet been cleared.</para>
    ///     <para xml:lang="ja">このエンティティが発行し、まだクリアされていないドメインイベントの一覧を取得する。</para>
    /// </summary>
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    /// <summary>
    ///     <para xml:lang="en">Clears the raised domain events. Called by the Unit of Work after a successful commit.</para>
    ///     <para xml:lang="ja">発行済みのドメインイベントをクリアする。Unit of WorkがCommit成功後に呼び出す。</para>
    /// </summary>
    void ClearDomainEvents();
}
