using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Tests.Helpers;

// テストヘルパー自体の検証。D3Fragments・ASP.NET Core を参照したコードがコンパイルでき、層の設定と複数プロジェクト構成が機能すること
public sealed class AnalyzerVerifierTests
{
    [Fact]
    public async Task VerifyAsync_CompilesCodeReferencingD3FragmentsAndAspNetCore()
    {
        const string source = """
            using D3Fragments;
            using D3Fragments.Application;
            using Microsoft.AspNetCore.Mvc;

            public sealed class Shelf : AggregateRoot { }
            public sealed record GetShelf(UuidV7 Id) : IQuery<int>;
            public sealed class ShelvesController(IRequestExecutor<GetShelf, int> executor) : ControllerBase { }
            """;

        await AnalyzerVerifier<NoOpAnalyzer>.VerifyAsync(source, ArchitectureLayerType.Presentation | ArchitectureLayerType.Domain);
    }

    [Fact]
    public async Task AddReferencedProject_MakesTypesOfReferencedProjectAvailable()
    {
        var test = AnalyzerVerifier<NoOpAnalyzer>.CreateTest("public sealed class Consumer { public Shelf? Shelf { get; } }", ArchitectureLayerType.Application);
        test.AddReferencedProject("DomainProject", "public sealed class Shelf : D3Fragments.AggregateRoot { }", ArchitectureLayerType.Domain);

        await test.RunAsync(TestContext.Current.CancellationToken);
    }

#pragma warning disable RS1036 // テスト専用のアナライザのため、拡張アナライザ規則の強制は不要
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoOpAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
        }
    }
#pragma warning restore RS1036
}
