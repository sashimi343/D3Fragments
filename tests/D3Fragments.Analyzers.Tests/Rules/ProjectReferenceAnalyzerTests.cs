using D3Fragments.Analyzers.Rules;
using D3Fragments.Analyzers.Tests.Helpers;
using Microsoft.CodeAnalysis.Testing;
using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.ProjectReferenceAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class ProjectReferenceAnalyzerTests
{
    private const string Source = "public sealed class Consumer { public Provided? Value { get; } }";
    private const string ReferencedSource = "public sealed class Provided { }";

    [Theory]
    [InlineData(ArchitectureLayerType.Application, ArchitectureLayerType.Presentation)]
    [InlineData(ArchitectureLayerType.Application, ArchitectureLayerType.Infrastructure)]
    [InlineData(ArchitectureLayerType.Domain, ArchitectureLayerType.Presentation)]
    [InlineData(ArchitectureLayerType.Domain, ArchitectureLayerType.Application)]
    [InlineData(ArchitectureLayerType.Domain, ArchitectureLayerType.Infrastructure)]
    [InlineData(ArchitectureLayerType.Infrastructure, ArchitectureLayerType.Presentation)]
    // 複数層：いずれの組み合わせでも許可されない
    [InlineData(ArchitectureLayerType.Application | ArchitectureLayerType.Domain, ArchitectureLayerType.Infrastructure)]
    public async Task ForbiddenReference_ReportsDiagnostic(ArchitectureLayerType from, ArchitectureLayerType to)
    {
        var test = CreateTest(from, to);
        test.ExpectedDiagnostics.Add(Verifier.Diagnostic(DiagnosticDescriptors.DFR0001)
            .WithNoLocation()
            .WithArguments(from.ToString().Replace(", ", " | "), "TestProject", to.ToString().Replace(", ", " | "), "Referenced"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(ArchitectureLayerType.Presentation, ArchitectureLayerType.Application)]
    [InlineData(ArchitectureLayerType.Presentation, ArchitectureLayerType.Domain)]
    [InlineData(ArchitectureLayerType.Presentation, ArchitectureLayerType.Infrastructure)]
    [InlineData(ArchitectureLayerType.Application, ArchitectureLayerType.Domain)]
    [InlineData(ArchitectureLayerType.Infrastructure, ArchitectureLayerType.Application)]
    [InlineData(ArchitectureLayerType.Infrastructure, ArchitectureLayerType.Domain)]
    [InlineData(ArchitectureLayerType.Domain, ArchitectureLayerType.Domain)]
    // 複数層：いずれかの組み合わせで許可されていれば許可する
    [InlineData(ArchitectureLayerType.Domain, ArchitectureLayerType.Application | ArchitectureLayerType.Domain)]
    [InlineData(ArchitectureLayerType.Application | ArchitectureLayerType.Domain, ArchitectureLayerType.Application)]
    // 属性が未設定の場合は無条件に許可する
    [InlineData(ArchitectureLayerType.None, ArchitectureLayerType.Presentation)]
    [InlineData(ArchitectureLayerType.Domain, ArchitectureLayerType.None)]
    public async Task AllowedReference_ReportsNothing(ArchitectureLayerType from, ArchitectureLayerType to) =>
        await CreateTest(from, to).RunAsync(TestContext.Current.CancellationToken);

    [Fact]
    public async Task GlobalAnalyzerConfig_SuppressesDiagnostic()
    {
        var test = CreateTest(ArchitectureLayerType.Domain, ArchitectureLayerType.Application);
        test.TestState.AnalyzerConfigFiles.Add(("/.globalconfig", "is_global = true\ndotnet_diagnostic.DFR0001.severity = none"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    // ドキュメントの記載どおり、ファイル単位の .editorconfig 設定では抑制されないこと
    [Fact]
    public async Task EditorConfigSection_DoesNotSuppressDiagnostic()
    {
        var test = CreateTest(ArchitectureLayerType.Domain, ArchitectureLayerType.Application);
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n[*]\ndotnet_diagnostic.DFR0001.severity = none"));
        test.ExpectedDiagnostics.Add(Verifier.Diagnostic(DiagnosticDescriptors.DFR0001)
            .WithNoLocation()
            .WithArguments("Domain", "TestProject", "Application", "Referenced"));

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

    private static Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<ProjectReferenceAnalyzer, EmptyCodeFixProvider, DefaultVerifier> CreateTest(
        ArchitectureLayerType from, ArchitectureLayerType to)
    {
        var test = Verifier.CreateTest(Source, from);
        test.AddReferencedProject("Referenced", ReferencedSource, to);
        return test;
    }
}
