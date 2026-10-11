using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace D3Fragments.Analyzers.Tests.Helpers;

/// <summary>
///     <para xml:lang="en">Creates compilations for testing the common features of analyzers without running an analyzer.</para>
///     <para xml:lang="ja">アナライザを実行せずに共通機能をテストするためのコンパイルを作成する。</para>
/// </summary>
internal static class TestCompilation
{
    public static CSharpCompilation Create(
        string source,
        ArchitectureLayerType layer = ArchitectureLayerType.None,
        string assemblyName = "TestAssembly",
        params MetadataReference[] additionalReferences)
    {
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source), CSharpSyntaxTree.ParseText(TestReferences.ArchitectureLayerSource(layer))],
            [.. TestReferences.All, .. additionalReferences],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.True(errors.Count == 0, string.Join(Environment.NewLine, errors));
        return compilation;
    }

    public static INamedTypeSymbol GetType(this Compilation compilation, string metadataName) =>
        compilation.GetTypeByMetadataName(metadataName) ?? throw new ArgumentException($"Type '{metadataName}' was not found.", nameof(metadataName));
}
