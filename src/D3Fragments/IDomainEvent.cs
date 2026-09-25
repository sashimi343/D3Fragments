namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Marker interface for a domain event.</para>
///     <para xml:lang="ja">ドメインイベントのマーカーインターフェース。</para>
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    ///     <para xml:lang="en">Gets the date and time when the event occurred.</para>
    ///     <para xml:lang="ja">イベントが発生した日時を取得する。</para>
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
