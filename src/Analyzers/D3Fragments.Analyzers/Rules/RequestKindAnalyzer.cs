using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0016: リクエスト型が ICommand と IQuery のどちらか一方だけを実装していない
// AddD3FragmentsApplication の登録時の検査と同じ判定を、コンパイル時に行う
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RequestKindAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0016);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            if (knownTypes.IRequest is null || knownTypes.ICommand is null || knownTypes.IQuery is null)
            {
                return;
            }
            start.RegisterSymbolAction(c => AnalyzeType(c, knownTypes), SymbolKind.NamedType);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, KnownTypes knownTypes)
    {
        var type = (INamedTypeSymbol)context.Symbol;

        // 抽象型は派生型でICommand/IQueryを付ける場合があるため、実際に実行される具象型だけを検査する
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct)
            || type.IsAbstract
            || !type.Implements(knownTypes.IRequest)
            || type.Implements(knownTypes.ICommand) != type.Implements(knownTypes.IQuery))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DFR0016,
            type.Locations[0],
            type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
    }
}
