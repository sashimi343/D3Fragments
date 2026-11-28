using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using D3Fragments.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0018: Command の Handler・Commit 前のドメインイベントハンドラが、Command を同じスコープで実行する IRequestExecutor を注入している
// DFR0019: Query の Handler が、Command を実行する IRequestExecutor（IIsolatedRequestExecutor を含む）を注入している
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RequestExecutorInjectionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0018, DiagnosticDescriptors.DFR0019);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            if (knownTypes.IRequestExecutor is null || knownTypes.ICommand is null)
            {
                return;
            }

            if (new ArchitectureLayerResolver(knownTypes).Resolve(start.Compilation.Assembly) == ArchitectureLayerType.None)
            {
                return;
            }

            start.RegisterSymbolAction(c => AnalyzeType(c, knownTypes), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, KnownTypes knownTypes)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class)
        {
            return;
        }

        var handledRequests = type.AllInterfaces
            .Where(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, knownTypes.IRequestHandler))
            .Select(i => i.TypeArguments[0])
            .ToList();
        var isQueryHandler = handledRequests.Any(r => r.Implements(knownTypes.IQuery));

        // Commit 後のハンドラは Commit が済んでいるため、同じスコープで Command を実行してよい
        var isCommandSide = handledRequests.Any(r => r.Implements(knownTypes.ICommand)) || type.Implements(knownTypes.IBeforeCommitDomainEventHandler);
        if (!isQueryHandler && !isCommandSide)
        {
            return;
        }

        foreach (var parameter in type.InstanceConstructors.SelectMany(c => c.Parameters))
        {
            if (parameter.Type.FindInterface(knownTypes.IRequestExecutor) is not { } executor)
            {
                continue;
            }

            // 型パラメータは Command か Query かを判定できない。Query は保存されないため同じスコープで実行してよい
            var request = executor.TypeArguments[0];
            if (request is ITypeParameterSymbol || !request.Implements(knownTypes.ICommand))
            {
                continue;
            }

            DiagnosticDescriptor descriptor;
            if (isQueryHandler)
            {
                descriptor = DiagnosticDescriptors.DFR0019;
            }
            else if (!parameter.Type.Implements(knownTypes.IIsolatedRequestExecutor))
            {
                descriptor = DiagnosticDescriptors.DFR0018;
            }
            else
            {
                continue;
            }

            var location = (parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) as ParameterSyntax)?.Type?.GetLocation()
                ?? parameter.Locations[0];
            context.ReportDiagnostic(Diagnostic.Create(
                descriptor,
                location,
                type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                parameter.Type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                request.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }
    }
}
