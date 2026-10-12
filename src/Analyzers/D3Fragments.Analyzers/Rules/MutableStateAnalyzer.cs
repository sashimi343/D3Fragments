using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace D3Fragments.Analyzers.Rules;

// DFR0003: エンティティの公開セッター / DFR0009: エンティティの変更可能なコレクション / DFR0010: 値オブジェクトの公開セッター
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MutableStateAnalyzer : DiagnosticAnalyzer
{
    private static readonly HashSet<string> ReadOnlyCollectionNames =
    [
        "ReadOnlyCollection",
        "ReadOnlyDictionary",
        "ReadOnlySet",
        "ReadOnlyObservableCollection",
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0003, DiagnosticDescriptors.DFR0009, DiagnosticDescriptors.DFR0010);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var knownTypes = KnownTypes.Create(start.Compilation);
            if (knownTypes.EntityBase is null && knownTypes.IValueObject is null)
            {
                return;
            }
            start.RegisterSymbolAction(c => AnalyzeMember(c, knownTypes), SymbolKind.Property, SymbolKind.Field);
        });
    }

    private static void AnalyzeMember(SymbolAnalysisContext context, KnownTypes knownTypes)
    {
        var member = context.Symbol;
        var type = member.ContainingType;
        var isEntity = type.InheritsFrom(knownTypes.EntityBase);
        var isValueObject = type.InheritsFrom(knownTypes.ValueObjectBase) || type.Implements(knownTypes.IValueObject);
        if ((!isEntity && !isValueObject) || member.IsImplicitlyDeclared)
        {
            return;
        }

        var (isWritable, isReadable, memberType) = member switch
        {
            IPropertySymbol { ExplicitInterfaceImplementations.Length: 0 } p => (
                p.SetMethod?.DeclaredAccessibility.IsAccessibleFromOutside() == true,
                p.GetMethod?.DeclaredAccessibility.IsAccessibleFromOutside() == true,
                p.Type),
            IFieldSymbol { IsConst: false } f => (
                !f.IsReadOnly && f.DeclaredAccessibility.IsAccessibleFromOutside(),
                f.DeclaredAccessibility.IsAccessibleFromOutside(),
                f.Type),
            _ => (false, false, null),
        };

        var typeName = type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        if (isWritable)
        {
            var descriptor = isEntity ? DiagnosticDescriptors.DFR0003 : DiagnosticDescriptors.DFR0010;
            context.ReportDiagnostic(Diagnostic.Create(descriptor, member.Locations[0], typeName, member.Name));
        }
        if (isEntity && isReadable && memberType is not null && IsMutableCollection(memberType, knownTypes))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DFR0009,
                member.Locations[0],
                typeName,
                member.Name,
                memberType.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }
    }

    private static bool IsMutableCollection(ITypeSymbol type, KnownTypes knownTypes)
    {
        if (type is IArrayTypeSymbol)
        {
            return true;
        }

        var ns = type.ContainingNamespace?.ToDisplayString();
        if (ns == "System.Collections.Immutable" || (ns == "System.Collections.ObjectModel" && ReadOnlyCollectionNames.Contains(type.Name)))
        {
            return false;
        }
        return type.Implements(knownTypes.ICollection) || type.Implements(knownTypes.IDictionary);
    }
}
