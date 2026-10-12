using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0004: 集約の一部を取得するリポジトリメソッド / DFR0005: 集約の一部を更新するリポジトリメソッド
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepositorySignatureAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0004, DiagnosticDescriptors.DFR0005);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            if (knownTypes.IRepository is null || knownTypes.EntityBase is null)
            {
                return;
            }
            start.RegisterSymbolAction(c => AnalyzeMethod(c, knownTypes), SymbolKind.Method);
        });
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context, KnownTypes knownTypes)
    {
        var method = (IMethodSymbol)context.Symbol;
        if (method.MethodKind != MethodKind.Ordinary
            || method.ContainingType.TypeKind != TypeKind.Interface
            || method.ContainingType.FindInterface(knownTypes.IRepository) is not { } repository)
        {
            return;
        }

        // ジェネリックなリポジトリの基底インターフェースは、集約ルートが確定しないため対象外
        if (repository.TypeArguments[0] is not INamedTypeSymbol aggregate)
        {
            return;
        }

        var methodName = method.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        var aggregateName = aggregate.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);

        if (FindForeignEntity(method.ReturnType, aggregate, knownTypes) is { } returned)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DFR0004,
                method.Locations[0],
                methodName,
                aggregateName,
                returned.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }

        foreach (var parameter in method.Parameters)
        {
            if (FindForeignEntity(parameter.Type, aggregate, knownTypes) is { } received)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DFR0005,
                    parameter.Locations[0],
                    methodName,
                    parameter.Name,
                    aggregateName,
                    received.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
            }
        }
    }

    // 型（配列の要素型・ジェネリック型の型引数を再帰的に含む）から、集約ルート以外のエンティティを探す
    private static ITypeSymbol? FindForeignEntity(ITypeSymbol type, INamedTypeSymbol aggregate, KnownTypes knownTypes)
    {
        switch (type)
        {
            case IArrayTypeSymbol array:
                return FindForeignEntity(array.ElementType, aggregate, knownTypes);
            case INamedTypeSymbol named:
                if (named.InheritsFrom(knownTypes.EntityBase) && named.FindBaseType(aggregate.OriginalDefinition) is null)
                {
                    return named;
                }
                return named.TypeArguments.Select(t => FindForeignEntity(t, aggregate, knownTypes)).FirstOrDefault(t => t is not null);
            default:
                return null;
        }
    }
}
