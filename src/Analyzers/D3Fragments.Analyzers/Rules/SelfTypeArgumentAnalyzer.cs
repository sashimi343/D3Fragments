using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0007: 自己参照型パラメーター TSelf には宣言している型自身を指定する
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SelfTypeArgumentAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DFR0007);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            INamedTypeSymbol[] selfTypedBases = [.. new[] { knownTypes.TypeSafeEnum, knownTypes.ValueObjectBase, knownTypes.IValueObject }.OfType<INamedTypeSymbol>()];
            if (selfTypedBases.Length > 0)
            {
                start.RegisterSymbolAction(c => AnalyzeType(c, selfTypedBases), SymbolKind.NamedType);
            }
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol[] selfTypedBases)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var directBases = type.BaseType is null ? type.Interfaces : type.Interfaces.Add(type.BaseType);

        foreach (var baseType in directBases)
        {
            if (!selfTypedBases.Contains(baseType.OriginalDefinition, SymbolEqualityComparer.Default))
            {
                continue;
            }

            var self = baseType.TypeArguments[0];
            if (self is ITypeParameterSymbol || SymbolEqualityComparer.Default.Equals(self, type))
            {
                continue;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DFR0007,
                type.Locations[0],
                type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                baseType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                self.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }
    }
}
