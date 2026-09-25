namespace D3Fragments;

/// <summary>
///     <para xml:lang="en">Represents a pure judgment that spans multiple entities or value objects and cannot naturally belong to a single aggregate. Not required for judgments that can be expressed as a method on an aggregate.</para>
///     <para xml:lang="ja">複数のEntityやValue Objectにまたがり、特定の集約だけへ自然に所属させられない純粋な判定を表す。集約のメソッドとして自然に表せる判定にまで使う必要はない。</para>
/// </summary>
/// <typeparam name="TContext">
///     <para xml:lang="en">A record that names and groups the objects the judgment is made against.</para>
///     <para xml:lang="ja">判定対象を名前付きでまとめたrecord型。</para>
/// </typeparam>
public interface IDomainRule<in TContext>
{
    /// <summary>
    ///     <para xml:lang="en">Checks whether <paramref name="context"/> satisfies this rule.</para>
    ///     <para xml:lang="ja"><paramref name="context"/>がこのルールを満たすかどうかを判定する。</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">The objects the judgment is made against.</para>
    ///     <para xml:lang="ja">判定対象。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A success result when the rule is satisfied; otherwise, a failure result holding the reason.</para>
    ///     <para xml:lang="ja">ルールを満たす場合は成功結果、満たさない場合は理由を保持する失敗結果。</para>
    /// </returns>
    Result Check(TContext context);
}
