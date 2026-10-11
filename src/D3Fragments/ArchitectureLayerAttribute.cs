namespace D3Fragments;

/// <summary>
///    <para xml:lang="en">Specifies the architecture layer to which an assembly belongs.</para>
///    <para xml:lang="ja">アセンブリが属するアーキテクチャ層を指定します。</para>
/// </summary>
/// <remarks>
///    <para xml:lang="en">D3Fragments.Analyzers uses this attribute to verify dependencies between architecture layers.</para>
///    <para xml:lang="ja">D3Fragments.Analyzers は、この属性を使用してアーキテクチャ層間の依存関係を検証します。</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class ArchitectureLayerAttribute : Attribute
{
    /// <summary>
    ///   <para xml:lang="en">Initializes a new instance of the <see cref="ArchitectureLayerAttribute"/> class with the specified architecture layer.</para>
    ///   <para xml:lang="ja">指定されたアーキテクチャ層を使用して、<see cref="ArchitectureLayerAttribute"/> クラスの新しいインスタンスを初期化します。</para>
    /// </summary>
    /// <param name="architectureLayer">
    ///   <para xml:lang="en">The architecture layer to which the assembly belongs. Multiple layers can be combined with <c>|</c>.</para>
    ///   <para xml:lang="ja">アセンブリが属するアーキテクチャ層。複数の層を <c>|</c> で連結して指定できます。</para>
    /// </param>
    public ArchitectureLayerAttribute(ArchitectureLayerType architectureLayer)
    {
        ArchitectureLayer = architectureLayer;
    }

    /// <summary>
    ///  <para xml:lang="en">Gets the architecture layer to which the assembly belongs.</para>
    ///  <para xml:lang="ja">アセンブリが属するアーキテクチャ層を取得します。</para>
    /// </summary>
    public ArchitectureLayerType ArchitectureLayer { get; }
}
