using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using D3Fragments.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0001: アーキテクチャ参照違反（プロジェクト間参照）
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProjectReferenceAnalyzer : DiagnosticAnalyzer
{
    // 参照元の層から参照を許可する層
    private static readonly Dictionary<ArchitectureLayerType, ArchitectureLayerType> AllowedReferences = new()
    {
        [ArchitectureLayerType.Presentation] = ArchitectureLayerType.Presentation | ArchitectureLayerType.Application | ArchitectureLayerType.Domain | ArchitectureLayerType.Infrastructure,
        [ArchitectureLayerType.Application] = ArchitectureLayerType.Application | ArchitectureLayerType.Domain,
        [ArchitectureLayerType.Domain] = ArchitectureLayerType.Domain,
        [ArchitectureLayerType.Infrastructure] = ArchitectureLayerType.Application | ArchitectureLayerType.Domain | ArchitectureLayerType.Infrastructure,
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DFR0001);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        var layers = new ArchitectureLayerResolver(KnownTypes.Create(context.Compilation));
        var fromLayers = layers.Resolve(context.Compilation.Assembly);
        if (fromLayers == ArchitectureLayerType.None)
        {
            return;
        }

        // 複数の層を持つ場合、いずれかの層の組み合わせで許可されていれば参照を許可する
        var allowed = AllowedReferences.Where(p => (fromLayers & p.Key) != 0).Aggregate(ArchitectureLayerType.None, (acc, p) => acc | p.Value);

        foreach (var reference in context.Compilation.SourceModule.ReferencedAssemblySymbols)
        {
            var toLayers = layers.Resolve(reference);
            if (toLayers != ArchitectureLayerType.None && (allowed & toLayers) == 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DFR0001,
                    Location.None,
                    Display(fromLayers),
                    context.Compilation.AssemblyName,
                    Display(toLayers),
                    reference.Name));
            }
        }
    }

    private static string Display(ArchitectureLayerType layers) => layers.ToString().Replace(", ", " | ");
}
