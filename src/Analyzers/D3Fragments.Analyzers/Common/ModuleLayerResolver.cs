using System.Collections.Concurrent;
using D3Fragments.Analyzers.Models;
using Microsoft.CodeAnalysis;

namespace D3Fragments.Analyzers.Common;

// クラス・インターフェースのモジュールレイヤーを判定する。判定規則は docs/ja/architecture.md の「モジュールレイヤー」を参照
internal sealed class ModuleLayerResolver
{
    private readonly ArchitectureLayerResolver _architectureLayers;
    private readonly (ModuleLayerType ModuleLayer, ArchitectureLayerType ArchitectureLayer, Func<INamedTypeSymbol, bool> Match)[] _rules;
    private readonly ConcurrentDictionary<INamedTypeSymbol, ModuleLayerType?> _cache = new(SymbolEqualityComparer.Default);

    public ModuleLayerResolver(KnownTypes knownTypes, ArchitectureLayerResolver architectureLayers)
    {
        _architectureLayers = architectureLayers;

        // ドキュメントの記載順。複数の規則に該当する場合は先に該当したものを採用する
        _rules =
        [
            (ModuleLayerType.Presentation_Controller, ArchitectureLayerType.Presentation,
                t => IsClass(t) && (t.InheritsFrom(knownTypes.PageBase) || t.InheritsFrom(knownTypes.ControllerBase) || t.Implements(knownTypes.IComponent))),
            (ModuleLayerType.Application_Request, ArchitectureLayerType.Application,
                t => IsClass(t) && (t.Implements(knownTypes.IRequest) || t.Implements(knownTypes.ICommand))),
            (ModuleLayerType.Application_RequestHandler, ArchitectureLayerType.Application,
                t => IsClass(t) && t.Implements(knownTypes.IRequestHandler)),
            (ModuleLayerType.Application_RequestExecutor, ArchitectureLayerType.Application,
                t => IsClass(t) && t.Implements(knownTypes.IRequestExecutor)),
            (ModuleLayerType.Application_DomainEventHandler, ArchitectureLayerType.Application,
                t => IsClass(t) && (t.Implements(knownTypes.IBeforeCommitDomainEventHandler) || t.Implements(knownTypes.IAfterCommitDomainEventHandler))),
            (ModuleLayerType.Application_PipelineBehavior, ArchitectureLayerType.Application,
                t => IsClass(t) && t.Implements(knownTypes.IPipelineBehavior)),
            (ModuleLayerType.Domain_Entity, ArchitectureLayerType.Domain,
                t => IsClass(t) && t.InheritsFrom(knownTypes.EntityBase)),
            (ModuleLayerType.Domain_ValueObject, ArchitectureLayerType.Domain,
                t => IsClass(t) && t.Implements(knownTypes.IValueObject)),
            (ModuleLayerType.Domain_RepositoryInterface, ArchitectureLayerType.Domain,
                t => t.TypeKind == TypeKind.Interface && t.Implements(knownTypes.IRepository)),
            (ModuleLayerType.Domain_DomainRule, ArchitectureLayerType.Domain,
                t => IsClass(t) && t.Implements(knownTypes.IDomainRule)),
            (ModuleLayerType.Infrastructure_RepositoryImplementation, ArchitectureLayerType.Infrastructure,
                t => IsClass(t) && t.Implements(knownTypes.IRepository)),
            (ModuleLayerType.Infrastructure_DbContext, ArchitectureLayerType.Infrastructure,
                t => IsClass(t) && t.InheritsFrom(knownTypes.DbContext)),
            (ModuleLayerType.Infrastructure_UnitOfWork, ArchitectureLayerType.Infrastructure,
                t => IsClass(t) && t.Implements(knownTypes.IUnitOfWork)),
        ];
    }

    // アセンブリに ArchitectureLayerAttribute が設定されていない場合は null を返す
    public ModuleLayerType? Resolve(INamedTypeSymbol type) => _cache.GetOrAdd(type.OriginalDefinition, ResolveCore);

    private ModuleLayerType? ResolveCore(INamedTypeSymbol type)
    {
        var architectureLayers = _architectureLayers.Resolve(type.ContainingAssembly);
        if (architectureLayers == ArchitectureLayerType.None)
        {
            return null;
        }

        foreach (var rule in _rules)
        {
            if ((architectureLayers & rule.ArchitectureLayer) != 0 && rule.Match(type))
            {
                return rule.ModuleLayer;
            }
        }

        // 複数のアーキテクチャ層を持つアセンブリでは、どの層の Other かを特定できない
        return architectureLayers switch
        {
            ArchitectureLayerType.Presentation => ModuleLayerType.Presentation_Other,
            ArchitectureLayerType.Application => ModuleLayerType.Application_Other,
            ArchitectureLayerType.Domain => ModuleLayerType.Domain_Other,
            ArchitectureLayerType.Infrastructure => ModuleLayerType.Infrastructure_Other,
            _ => ModuleLayerType.Other,
        };
    }

    private static bool IsClass(INamedTypeSymbol type) => type.TypeKind == TypeKind.Class;
}
