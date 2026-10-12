using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using D3Fragments.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0002: アーキテクチャ参照違反（モジュール間参照）
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ModuleReferenceAnalyzer : DiagnosticAnalyzer
{
    private static readonly ModuleLayerType[] ApplicationLayers =
    [
        ModuleLayerType.Application_Request,
        ModuleLayerType.Application_RequestHandler,
        ModuleLayerType.Application_RequestExecutor,
        ModuleLayerType.Application_DomainEventHandler,
    ];

    // 参照元のモジュールレイヤーから参照を禁止するモジュールレイヤー
    private static readonly Dictionary<ModuleLayerType, HashSet<ModuleLayerType>> ForbiddenReferences = new()
    {
        [ModuleLayerType.Presentation_Controller] =
        [
            ModuleLayerType.Application_RequestHandler,
            ModuleLayerType.Application_DomainEventHandler,
            ModuleLayerType.Domain_RepositoryInterface,
            ModuleLayerType.Domain_DomainRule,
            ModuleLayerType.Infrastructure_RepositoryImplementation,
            ModuleLayerType.Infrastructure_DbContext,
            ModuleLayerType.Infrastructure_UnitOfWork,
            ModuleLayerType.Infrastructure_Other,
        ],
        [ModuleLayerType.Application_Request] = [ModuleLayerType.Presentation_Controller, ModuleLayerType.Infrastructure_RepositoryImplementation],
        [ModuleLayerType.Application_RequestHandler] = [ModuleLayerType.Presentation_Controller, ModuleLayerType.Infrastructure_RepositoryImplementation],
        [ModuleLayerType.Application_RequestExecutor] = [ModuleLayerType.Presentation_Controller, ModuleLayerType.Infrastructure_RepositoryImplementation],
        [ModuleLayerType.Application_DomainEventHandler] = [ModuleLayerType.Presentation_Controller, ModuleLayerType.Infrastructure_RepositoryImplementation],
        [ModuleLayerType.Domain_Entity] =
            [.. ApplicationLayers, ModuleLayerType.Presentation_Controller, ModuleLayerType.Domain_RepositoryInterface, ModuleLayerType.Infrastructure_RepositoryImplementation],
        [ModuleLayerType.Domain_ValueObject] =
            [.. ApplicationLayers, ModuleLayerType.Presentation_Controller, ModuleLayerType.Domain_RepositoryInterface, ModuleLayerType.Infrastructure_RepositoryImplementation],
        [ModuleLayerType.Domain_RepositoryInterface] =
            [.. ApplicationLayers, ModuleLayerType.Presentation_Controller, ModuleLayerType.Infrastructure_RepositoryImplementation],
        [ModuleLayerType.Domain_DomainRule] =
            [.. ApplicationLayers, ModuleLayerType.Presentation_Controller, ModuleLayerType.Domain_RepositoryInterface, ModuleLayerType.Infrastructure_RepositoryImplementation],
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DFR0002);

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
            start.RegisterSyntaxNodeAction(c => AnalyzeName(c, moduleLayers), SyntaxKind.IdentifierName, SyntaxKind.GenericName);
        });
    }

    private static void AnalyzeName(SyntaxNodeAnalysisContext context, ModuleLayerResolver moduleLayers)
    {
        var from = context.ContainingSymbol as INamedTypeSymbol ?? context.ContainingSymbol?.ContainingType;
        if (from is null || moduleLayers.Resolve(from) is not { } fromLayer || !ForbiddenReferences.TryGetValue(fromLayer, out var forbidden))
        {
            return;
        }

        var to = ReferencedType(context.SemanticModel.GetSymbolInfo(context.Node, context.CancellationToken).Symbol);
        if (to is null
            || SymbolEqualityComparer.Default.Equals(to.OriginalDefinition, from.OriginalDefinition)
            || moduleLayers.Resolve(to) is not { } toLayer
            || !forbidden.Contains(toLayer)
            || IsInNameof(context))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DFR0002,
            context.Node.GetLocation(),
            fromLayer,
            from.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
            toLayer,
            to.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
    }

    // 型の参照はその型、メンバーの参照（呼び出し・属性のコンストラクタ等）はメンバーを宣言している型を参照先とする
    private static INamedTypeSymbol? ReferencedType(ISymbol? symbol) => symbol switch
    {
        INamedTypeSymbol type => type,
        IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol => symbol.ContainingType,
        _ => null,
    };

    private static bool IsInNameof(SyntaxNodeAnalysisContext context) =>
        context.Node.Ancestors().OfType<InvocationExpressionSyntax>().Any(invocation =>
            invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" }
            && context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is null);
}
