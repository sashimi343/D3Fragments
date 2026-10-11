using System.Collections.Concurrent;
using D3Fragments.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace D3Fragments.Analyzers.Common;

// アセンブリに設定された ArchitectureLayerAttribute からアーキテクチャ層を取得する
internal sealed class ArchitectureLayerResolver(KnownTypes knownTypes)
{
    private readonly ConcurrentDictionary<IAssemblySymbol, ArchitectureLayerType> _cache = new(SymbolEqualityComparer.Default);

    // 属性が未設定の場合は ArchitectureLayerType.None を返す
    public ArchitectureLayerType Resolve(IAssemblySymbol assembly) =>
        knownTypes.ArchitectureLayerAttribute is null ? ArchitectureLayerType.None : _cache.GetOrAdd(assembly, ResolveCore);

    private ArchitectureLayerType ResolveCore(IAssemblySymbol assembly)
    {
        var attribute = assembly.GetAttributes().FirstOrDefault(a =>
            SymbolEqualityComparer.Default.Equals(a.AttributeClass, knownTypes.ArchitectureLayerAttribute));
        return attribute is not null && attribute.ConstructorArguments.Length == 1 && attribute.ConstructorArguments[0].Value is int value
            ? (ArchitectureLayerType)value
            : ArchitectureLayerType.None;
    }
}
