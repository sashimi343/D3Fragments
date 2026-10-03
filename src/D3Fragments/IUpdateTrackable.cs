namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Implemented by an entity that automatically tracks its last update timestamp and a version number used for optimistic concurrency control.</para>
///     <para xml:lang="ja">更新日時と、楽観的同時実行制御に使用するバージョン番号の自動追跡が可能なエンティティが実装するインターフェース。</para>
/// </summary>
public interface IUpdateTrackable
{
    /// <summary>
    ///     <para xml:lang="en">Gets the timestamp of the last update.</para>
    ///     <para xml:lang="ja">最終更新日時を取得する。</para>
    /// </summary>
    DateTimeOffset UpdatedAt { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the version number, incremented on every update. Used as the concurrency token for optimistic concurrency control.</para>
    ///     <para xml:lang="ja">更新のたびに加算されるバージョン番号を取得する。楽観的同時実行制御の同時実行トークンとして使用する。</para>
    /// </summary>
    long Version { get; }

    /// <summary>
    ///     <para xml:lang="en">Marks this entity as updated: sets the last-update timestamp to the current system time (UTC) and increments <see cref="Version"/>.</para>
    ///     <para xml:lang="ja">このエンティティを更新済みにする。更新日時を現在のシステム日時（UTC）にし、<see cref="Version"/>を1加算する。</para>
    /// </summary>
    void MarkUpdated();
}
