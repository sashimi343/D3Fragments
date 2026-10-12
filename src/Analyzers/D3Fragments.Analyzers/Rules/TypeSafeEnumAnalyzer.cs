using System.Collections.Immutable;
using D3Fragments.Analyzers.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace D3Fragments.Analyzers.Rules;

// DFR0006: TypeSafeEnum 型のコンストラクタの公開 / DFR0008: TypeSafeEnum の値の定義方法
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeSafeEnumAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.DFR0006, DiagnosticDescriptors.DFR0008);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(start =>
        {
            var typeSafeEnum = KnownTypes.Create(start.Compilation).TypeSafeEnum;
            if (typeSafeEnum is null)
            {
                return;
            }
            start.RegisterSymbolAction(c => AnalyzeType(c, typeSafeEnum), SymbolKind.NamedType);
            start.RegisterSymbolAction(c => AnalyzeField(c, typeSafeEnum), SymbolKind.Field);
            start.RegisterOperationAction(c => AnalyzeObjectCreation(c, typeSafeEnum), OperationKind.ObjectCreation);
        });
    }

    private static void AnalyzeType(SymbolAnalysisContext context, INamedTypeSymbol typeSafeEnum)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        var typeName = type.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat);

        if (IsTypeSafeEnumType(type, typeSafeEnum))
        {
            foreach (var constructor in type.InstanceConstructors.Where(c => !c.IsImplicitlyDeclared))
            {
                if (constructor.DeclaredAccessibility != Accessibility.Private || IsPrimaryConstructor(constructor))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DFR0006,
                        constructor.Locations[0],
                        constructor.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat),
                        typeName));
                }
            }
            return;
        }

        // TypeSafeEnum 型の派生クラスは、その TypeSafeEnum 型の private な入れ子クラスでなければならない
        if (FindTypeSafeEnumType(type.BaseType, typeSafeEnum) is { } owner
            && !(SymbolEqualityComparer.Default.Equals(type.ContainingType, owner) && type.DeclaredAccessibility == Accessibility.Private))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DFR0006,
                type.Locations[0],
                typeName,
                owner.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
        }
    }

    private static void AnalyzeField(SymbolAnalysisContext context, INamedTypeSymbol typeSafeEnum)
    {
        var field = (IFieldSymbol)context.Symbol;
        if (!field.IsStatic || field.IsReadOnly || field.IsConst || field.IsImplicitlyDeclared
            || FindTypeSafeEnumType(field.Type as INamedTypeSymbol, typeSafeEnum) is not { } owner
            || !IsWithin(field.ContainingType, owner))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DFR0008,
            field.Locations[0],
            owner.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context, INamedTypeSymbol typeSafeEnum)
    {
        var creation = (IObjectCreationOperation)context.Operation;
        if (FindTypeSafeEnumType(creation.Type as INamedTypeSymbol, typeSafeEnum) is not { } owner
            || !IsWithin(context.ContainingSymbol.ContainingType, owner))
        {
            return;
        }

        // 値の定義として許可するのは TypeSafeEnum 型の static readonly フィールドの初期化子のみ。
        // readonly でない static フィールドはフィールド側で報告するため、ここでは報告しない
        if (context.ContainingSymbol is IFieldSymbol { IsStatic: true } field
            && SymbolEqualityComparer.Default.Equals(field.ContainingType, owner))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DFR0008,
            creation.Syntax.GetLocation(),
            owner.ToDisplayString(SymbolDisplayFormat.CSharpShortErrorMessageFormat)));
    }

    // 基底クラスとして TypeSafeEnum<TSelf, TKey> を直接指定したクラス（TSelf が型パラメーターの場合を除く）
    // 構築済みの型では TSelf が置き換わっているため、元の定義の基底クラスで判定する
    private static bool IsTypeSafeEnumType(INamedTypeSymbol type, INamedTypeSymbol typeSafeEnum) =>
        type.OriginalDefinition.BaseType is { } baseType
        && SymbolEqualityComparer.Default.Equals(baseType.OriginalDefinition, typeSafeEnum)
        && baseType.TypeArguments[0] is not ITypeParameterSymbol;

    // type 自身または基底クラスのうち TypeSafeEnum 型であるものを返す
    private static INamedTypeSymbol? FindTypeSafeEnumType(INamedTypeSymbol? type, INamedTypeSymbol typeSafeEnum)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (IsTypeSafeEnumType(current, typeSafeEnum))
            {
                return current;
            }
        }
        return null;
    }

    private static bool IsWithin(INamedTypeSymbol? type, INamedTypeSymbol owner)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, owner))
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsPrimaryConstructor(IMethodSymbol constructor) =>
        constructor.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax);
}
