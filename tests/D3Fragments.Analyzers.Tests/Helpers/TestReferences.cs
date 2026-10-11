using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace D3Fragments.Analyzers.Tests.Helpers;

/// <summary>
///     <para xml:lang="en">References used to compile test code: the assemblies loaded by the running test (the .NET and ASP.NET Core runtimes, D3Fragments, and D3Fragments.Application).</para>
///     <para xml:lang="ja">テスト対象コードのコンパイルに使う参照。実行中のテストが読み込むアセンブリ（.NET・ASP.NET Core ランタイム、D3Fragments、D3Fragments.Application）をそのまま使う。</para>
/// </summary>
internal static class TestReferences
{
    public static ImmutableArray<MetadataReference> All { get; } =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .ToImmutableArray();

    // 参照は All で与えるため、テストライブラリによる参照アセンブリの取得は行わない
    public static ReferenceAssemblies None { get; } = new("net10.0");

    public static string ArchitectureLayerSource(ArchitectureLayerType layer) =>
        layer == ArchitectureLayerType.None
            ? string.Empty
            : $"[assembly: D3Fragments.ArchitectureLayer({string.Join(" | ", layer.ToString().Split(", ").Select(l => "D3Fragments.ArchitectureLayerType." + l))})]";
}
