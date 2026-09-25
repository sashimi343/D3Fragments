namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Implemented by an entity that automatically tracks its last update timestamp.</para>
///     <para xml:lang="ja">更新日時の自動追跡が可能なエンティティが実装するインターフェース。</para>
/// </summary>
public interface IUpdateTrackable
{
    /// <summary>
    ///     <para xml:lang="en">Gets the timestamp of the last update.</para>
    ///     <para xml:lang="ja">最終更新日時を取得する。</para>
    /// </summary>
    DateTimeOffset UpdatedAt { get; }

    /// <summary>
    ///     <para xml:lang="en">Updates the last-update timestamp to the specified time.</para>
    ///     <para xml:lang="ja">更新日時を指定した日時に更新する。</para>
    /// </summary>
    /// <param name="now">
    ///     <para xml:lang="en">The timestamp to set.</para>
    ///     <para xml:lang="ja">設定する日時。</para>
    /// </param>
    void UpdateTimestamp(DateTimeOffset now);
}
