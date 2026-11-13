using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0015: 子エンティティを含む集約のリポジトリで FindAsync をオーバーライドしていない
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RepositoryFindAsyncAnalyzer : DiagnosticAnalyzer
{
    private const string FindAsyncName = "FindAsync";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0015);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            if (knownTypes.RepositoryBase is null || knownTypes.EntityBase is null || knownTypes.AggregateRoot is null)
            {
                return;
            }
            start.RegisterSymbolAction(c => AnalyzeType(c, knownTypes), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, KnownTypes knownTypes)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class
            || type.IsAbstract
            || type.FindBaseType(knownTypes.RepositoryBase) is not { } repositoryBase
            || repositoryBase.TypeArguments[1] is not INamedTypeSymbol aggregate
            || OverridesFindAsync(type, repositoryBase))
        {
            return;
        }

        var child = FindChildEntityProperty(aggregate, knownTypes);
        if (child is null)
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DFR0015,
            type.Locations[0],
            type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
            aggregate.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
            child.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
    }

    // 派生クラス自身か、RepositoryBase までの中間の基底クラスがオーバーライドしていればよい
    private static bool OverridesFindAsync(INamedTypeSymbol type, INamedTypeSymbol repositoryBase)
    {
        for (var current = type; current is not null && !SymbolEqualityComparer.Default.Equals(current, repositoryBase); current = current.BaseType)
        {
            if (current.GetMembers(FindAsyncName).OfType<IMethodSymbol>().Any(m => m.IsOverride))
            {
                return true;
            }
        }
        return false;
    }

    // 集約（基底クラスを含む）のインスタンスプロパティから、子エンティティ（集約ルート以外のエンティティ）を参照するものを探す
    private static IPropertySymbol? FindChildEntityProperty(INamedTypeSymbol aggregate, KnownTypes knownTypes)
    {
        for (var current = aggregate; current is not null; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!property.IsStatic && ContainsChildEntity(property.Type, knownTypes))
                {
                    return property;
                }
            }
        }
        return null;
    }

    private static bool ContainsChildEntity(ITypeSymbol type, KnownTypes knownTypes) => type switch
    {
        IArrayTypeSymbol array => ContainsChildEntity(array.ElementType, knownTypes),
        INamedTypeSymbol named => (named.InheritsFrom(knownTypes.EntityBase) && !named.InheritsFrom(knownTypes.AggregateRoot))
            || named.TypeArguments.Any(t => ContainsChildEntity(t, knownTypes)),
        _ => false,
    };
}
