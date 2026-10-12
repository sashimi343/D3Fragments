using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using D3Fragments.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0011: コントローラはドメイン層のクラスをコンストラクタで注入すべきではない
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ControllerInjectionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DFR0011);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            var architectureLayers = new ArchitectureLayerResolver(knownTypes);
            if (architectureLayers.Resolve(start.Compilation.Assembly) == ArchitectureLayerType.None)
            {
                return;
            }

            var moduleLayers = new ModuleLayerResolver(knownTypes, architectureLayers);
            start.RegisterSymbolAction(c => AnalyzeType(c, moduleLayers), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, ModuleLayerResolver moduleLayers)
    {
        var controller = (INamedTypeSymbol)context.Symbol;
        if (moduleLayers.Resolve(controller) != ModuleLayerType.Presentation_Controller)
        {
            return;
        }

        // Domain_Other は TypeSafeEnum・エラー定義等も含むため、参照全般ではなくDIによる注入（コンストラクタ引数）に限って検出する。
        // DIで注入される Domain_Other はドメインサービスとみなせる
        foreach (var parameter in controller.InstanceConstructors.SelectMany(c => c.Parameters))
        {
            if (parameter.Type is not INamedTypeSymbol type || moduleLayers.Resolve(type) != ModuleLayerType.Domain_Other)
            {
                continue;
            }

            var location = (parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) as ParameterSyntax)?.Type?.GetLocation()
                ?? parameter.Locations[0];
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DFR0011,
                location,
                controller.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }
    }
}
