using Microsoft.CodeAnalysis;

namespace D3Fragments.Analyzers.Common;

internal static class SymbolExtensions
{
    // 基底クラスを辿り、定義が baseDefinition である構築済みの型を返す（type 自身を含む）
    public static INamedTypeSymbol? FindBaseType(this ITypeSymbol type, INamedTypeSymbol? baseDefinition)
    {
        if (baseDefinition is null)
        {
            return null;
        }
        for (var current = type as INamedTypeSymbol; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseDefinition))
            {
                return current;
            }
        }
        return null;
    }

    // 実装・継承しているインターフェースのうち、定義が interfaceDefinition である構築済みの型を返す（type 自身を含む）
    public static INamedTypeSymbol? FindInterface(this ITypeSymbol type, INamedTypeSymbol? interfaceDefinition)
    {
        if (interfaceDefinition is null)
        {
            return null;
        }
        if (type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, interfaceDefinition))
        {
            return named;
        }
        return type.AllInterfaces.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, interfaceDefinition));
    }

    public static bool InheritsFrom(this ITypeSymbol type, INamedTypeSymbol? baseDefinition) =>
        type.FindBaseType(baseDefinition) is not null;

    public static bool Implements(this ITypeSymbol type, INamedTypeSymbol? interfaceDefinition) =>
        type.FindInterface(interfaceDefinition) is not null;

    // 宣言しているクラスの外部（同一アセンブリを含む）からアクセスできるか
    public static bool IsAccessibleFromOutside(this Accessibility accessibility) =>
        accessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;
}
