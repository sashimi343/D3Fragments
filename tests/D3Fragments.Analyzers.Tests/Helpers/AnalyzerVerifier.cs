using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace D3Fragments.Analyzers.Tests.Helpers;

/// <summary>
///     <para xml:lang="en">Verifies the diagnostics reported by an analyzer.</para>
///     <para xml:lang="ja">アナライザが報告する診断を検証する。</para>
/// </summary>
internal static class AnalyzerVerifier<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor) => new(descriptor);

    public static Task VerifyAsync(string source, params DiagnosticResult[] expected) =>
        VerifyAsync(source, ArchitectureLayerType.None, expected);

    // layer を指定すると、テスト対象のアセンブリに ArchitectureLayerAttribute を設定する
    public static Task VerifyAsync(string source, ArchitectureLayerType layer, params DiagnosticResult[] expected)
    {
        var test = CreateTest(source, layer);
        test.ExpectedDiagnostics.AddRange(expected);
        return test.RunAsync(TestContext.Current.CancellationToken);
    }

    // 複数プロジェクト構成などを追加で設定する場合に使う（コード修正は行わない）
    public static CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier> CreateTest(string source, ArchitectureLayerType layer = ArchitectureLayerType.None)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, EmptyCodeFixProvider, DefaultVerifier>
        {
            TestCode = source,
            ReferenceAssemblies = TestReferences.None,
        };
        test.TestState.AdditionalReferences.AddRange(TestReferences.All);
        if (layer != ArchitectureLayerType.None)
        {
            test.TestState.Sources.Add(("ArchitectureLayer.cs", TestReferences.ArchitectureLayerSource(layer)));
        }
        return test;
    }
}

internal static class AnalyzerTestExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Adds a project referenced by the test project.</para>
    ///     <para xml:lang="ja">テスト対象のプロジェクトから参照されるプロジェクトを追加する。</para>
    /// </summary>
    public static void AddReferencedProject(this AnalyzerTest<DefaultVerifier> test, string name, string source, ArchitectureLayerType layer = ArchitectureLayerType.None)
    {
        var project = new ProjectState(name, LanguageNames.CSharp, "/" + name + "/Test", "cs")
        {
            ReferenceAssemblies = TestReferences.None,
        };
        project.Sources.Add(source);
        if (layer != ArchitectureLayerType.None)
        {
            project.Sources.Add(TestReferences.ArchitectureLayerSource(layer));
        }
        project.AdditionalReferences.AddRange(TestReferences.All);
        test.TestState.AdditionalProjects.Add(name, project);
        test.TestState.AdditionalProjectReferences.Add(name);
    }
}
