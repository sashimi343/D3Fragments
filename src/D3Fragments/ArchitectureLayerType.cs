namespace D3Fragments;

/// <summary>
///    <para xml:lang="en">An enumeration that represents the type of architecture layer to which an assembly belongs.</para>
///    <para xml:lang="ja">アセンブリが属するアーキテクチャ層の種類を表す列挙型です。</para>
/// </summary>
[Flags]
public enum ArchitectureLayerType
{
    /// <summary>
    ///   <para xml:lang="en">Indicates that the assembly does not belong to any specific architecture layer.</para>
    ///   <para xml:lang="ja">アセンブリが特定のアーキテクチャ層に属さないことを示します。</para>
    /// </summary>
    None = 0,

    /// <summary>
    ///  <para xml:lang="en">Indicates that the assembly belongs to the Presentation layer.</para>
    ///  <para xml:lang="ja">アセンブリがプレゼンテーション層（ユーザインターフェース層）に属することを示します。</para>
    /// </summary>
    Presentation = 1 << 0,

    /// <summary>
    ///  <para xml:lang="en">Indicates that the assembly belongs to the Application layer.</para>
    ///  <para xml:lang="ja">アセンブリがアプリケーション層に属することを示します。</para>
    /// </summary>
    Application = 1 << 1,

    /// <summary>
    ///  <para xml:lang="en">Indicates that the assembly belongs to the Domain layer.</para>
    ///  <para xml:lang="ja">アセンブリがドメイン層に属することを示します。</para>
    /// </summary>
    Domain = 1 << 2,

    /// <summary>
    ///  <para xml:lang="en">Indicates that the assembly belongs to the Infrastructure layer.</para>
    ///  <para xml:lang="ja">アセンブリがインフラストラクチャ層に属することを示します。</para>
    /// </summary>
    Infrastructure = 1 << 3,
}
