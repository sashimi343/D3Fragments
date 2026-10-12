using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace D3Fragments.Analyzers.Tests.Helpers;

/// <summary>
///     <para xml:lang="en">Verifies the result of applying a code fix.</para>
///     <para xml:lang="ja">コード修正を適用した結果を検証する。</para>
/// </summary>
internal static class CodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    public static Task VerifyAsync(string source, string fixedSource)
    {
        var test = new CSharpCodeFixTest<TAnalyzer, TCodeFix, DefaultVerifier>
        {
            TestCode = source,
            FixedCode = fixedSource,
            ReferenceAssemblies = TestReferences.None,
        };
        test.TestState.AdditionalReferences.AddRange(TestReferences.All);
        // 修正後も残る診断（コード修正の対象外のフィールド等）を FixedCode のマークアップで検証できるようにする
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        return test.RunAsync(TestContext.Current.CancellationToken);
    }
}
