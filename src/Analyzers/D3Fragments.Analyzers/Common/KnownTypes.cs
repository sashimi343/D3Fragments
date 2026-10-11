using Microsoft.CodeAnalysis;

namespace D3Fragments.Analyzers.Common;

// 規則の判定基準となる型。参照されていない型は null になる
internal sealed class KnownTypes
{
    private KnownTypes(Compilation compilation)
    {
        ArchitectureLayerAttribute = compilation.GetTypeByMetadataName("D3Fragments.ArchitectureLayerAttribute");
        EntityBase = compilation.GetTypeByMetadataName("D3Fragments.EntityBase`1");
        IValueObject = compilation.GetTypeByMetadataName("D3Fragments.IValueObject`2");
        ValueObjectBase = compilation.GetTypeByMetadataName("D3Fragments.ValueObjectBase`2");
        IRepository = compilation.GetTypeByMetadataName("D3Fragments.IRepository`2");
        IDomainRule = compilation.GetTypeByMetadataName("D3Fragments.IDomainRule`1");
        TypeSafeEnum = compilation.GetTypeByMetadataName("D3Fragments.TypeSafeEnum`2");
        IRequest = compilation.GetTypeByMetadataName("D3Fragments.Application.IRequest`1");
        ICommand = compilation.GetTypeByMetadataName("D3Fragments.Application.ICommand");
        IRequestHandler = compilation.GetTypeByMetadataName("D3Fragments.Application.IRequestHandler`2");
        IRequestExecutor = compilation.GetTypeByMetadataName("D3Fragments.Application.IRequestExecutor`2");
        IDomainEventHandler = compilation.GetTypeByMetadataName("D3Fragments.Application.IDomainEventHandler`1");
        ControllerBase = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
        PageBase = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.RazorPages.PageBase");
        IComponent = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");
    }

    public INamedTypeSymbol? ArchitectureLayerAttribute { get; }
    public INamedTypeSymbol? EntityBase { get; }
    public INamedTypeSymbol? IValueObject { get; }
    public INamedTypeSymbol? ValueObjectBase { get; }
    public INamedTypeSymbol? IRepository { get; }
    public INamedTypeSymbol? IDomainRule { get; }
    public INamedTypeSymbol? TypeSafeEnum { get; }
    public INamedTypeSymbol? IRequest { get; }
    public INamedTypeSymbol? ICommand { get; }
    public INamedTypeSymbol? IRequestHandler { get; }
    public INamedTypeSymbol? IRequestExecutor { get; }
    public INamedTypeSymbol? IDomainEventHandler { get; }
    public INamedTypeSymbol? ControllerBase { get; }
    public INamedTypeSymbol? PageBase { get; }
    public INamedTypeSymbol? IComponent { get; }

    public static KnownTypes Create(Compilation compilation) => new(compilation);
}
