using Xunit;

namespace D3Fragments.Testing;

/// <summary>
///     <para xml:lang="en">Provides assertions for raised domain events.</para>
///     <para xml:lang="ja">発火したドメインイベントに対するアサーションを提供する。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Pass an aggregate to verify the events it holds, or pass <see cref="SpyDomainEventDispatcher.DispatchedEvents"/> to verify the events dispatched after saving.</para>
///     <para xml:lang="ja">集約を渡すと集約が保持するイベントを、<see cref="SpyDomainEventDispatcher.DispatchedEvents"/>を渡すと保存後に発火されたイベントを検証する。</para>
/// </remarks>
public static class DomainEventAssert
{
    /// <summary>
    ///     <para xml:lang="en">Verifies that the source holds exactly one domain event of the specified type and returns it.</para>
    ///     <para xml:lang="ja">指定した型のドメインイベントをちょうど1つ保持していることを検証し、そのイベントを返す。</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">The type of the expected domain event.</para>
    ///     <para xml:lang="ja">期待するドメインイベントの型。</para>
    /// </typeparam>
    /// <param name="source">
    ///     <para xml:lang="en">The aggregate that holds domain events.</para>
    ///     <para xml:lang="ja">ドメインイベントを保持する集約。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The domain event of the specified type.</para>
    ///     <para xml:lang="ja">指定した型のドメインイベント。</para>
    /// </returns>
    public static TEvent Raised<TEvent>(IHasDomainEvents source)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(source);
        return Raised<TEvent>(source.DomainEvents);
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies that the events contain exactly one domain event of the specified type and returns it.</para>
    ///     <para xml:lang="ja">指定した型のドメインイベントがちょうど1つ含まれることを検証し、そのイベントを返す。</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">The type of the expected domain event.</para>
    ///     <para xml:lang="ja">期待するドメインイベントの型。</para>
    /// </typeparam>
    /// <param name="events">
    ///     <para xml:lang="en">The domain events to verify.</para>
    ///     <para xml:lang="ja">検証対象のドメインイベント。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The domain event of the specified type.</para>
    ///     <para xml:lang="ja">指定した型のドメインイベント。</para>
    /// </returns>
    public static TEvent Raised<TEvent>(IEnumerable<IDomainEvent> events)
        where TEvent : IDomainEvent
        => (TEvent)Assert.Single(events, e => e is TEvent);

    /// <summary>
    ///     <para xml:lang="en">Verifies that the source holds no domain event of the specified type.</para>
    ///     <para xml:lang="ja">指定した型のドメインイベントを保持していないことを検証する。</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">The type of the domain event that must not be raised.</para>
    ///     <para xml:lang="ja">発火していないことを期待するドメインイベントの型。</para>
    /// </typeparam>
    /// <param name="source">
    ///     <para xml:lang="en">The aggregate that holds domain events.</para>
    ///     <para xml:lang="ja">ドメインイベントを保持する集約。</para>
    /// </param>
    public static void NotRaised<TEvent>(IHasDomainEvents source)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(source);
        NotRaised<TEvent>(source.DomainEvents);
    }

    /// <summary>
    ///     <para xml:lang="en">Verifies that the events contain no domain event of the specified type.</para>
    ///     <para xml:lang="ja">指定した型のドメインイベントが含まれないことを検証する。</para>
    /// </summary>
    /// <typeparam name="TEvent">
    ///     <para xml:lang="en">The type of the domain event that must not be raised.</para>
    ///     <para xml:lang="ja">発火していないことを期待するドメインイベントの型。</para>
    /// </typeparam>
    /// <param name="events">
    ///     <para xml:lang="en">The domain events to verify.</para>
    ///     <para xml:lang="ja">検証対象のドメインイベント。</para>
    /// </param>
    public static void NotRaised<TEvent>(IEnumerable<IDomainEvent> events)
        where TEvent : IDomainEvent
        => Assert.DoesNotContain(events, e => e is TEvent);
}
