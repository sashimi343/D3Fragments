using Microsoft.CodeAnalysis;

namespace D3Fragments.Analyzers.Common;

// 規則の判定基準となる型。参照されていない型は null になる
internal sealed class KnownTypes
{
    private KnownTypes(Compilation compilation)
    {
        ArchitectureLayerAttribute = compilation.GetTypeByMetadataName("D3Fragments.ArchitectureLayerAttribute");
        EntityBase = compilation.GetTypeByMetadataName("D3Fragments.EntityBase`1");
        AggregateRoot = compilation.GetTypeByMetadataName("D3Fragments.AggregateRoot`1");
        IValueObject = compilation.GetTypeByMetadataName("D3Fragments.IValueObject`2");
        ValueObjectBase = compilation.GetTypeByMetadataName("D3Fragments.ValueObjectBase`2");
        IRepository = compilation.GetTypeByMetadataName("D3Fragments.IRepository`2");
        IDomainRule = compilation.GetTypeByMetadataName("D3Fragments.IDomainRule`1");
        TypeSafeEnum = compilation.GetTypeByMetadataName("D3Fragments.TypeSafeEnum`2");
        IRequest = compilation.GetTypeByMetadataName("D3Fragments.Application.IRequest`1");
        ICommand = compilation.GetTypeByMetadataName("D3Fragments.Application.ICommand");
        IQuery = compilation.GetTypeByMetadataName("D3Fragments.Application.IQuery");
        IRequestHandler = compilation.GetTypeByMetadataName("D3Fragments.Application.IRequestHandler`2");
        IRequestExecutor = compilation.GetTypeByMetadataName("D3Fragments.Application.IRequestExecutor`2");
        IBeforeCommitDomainEventHandler = compilation.GetTypeByMetadataName("D3Fragments.Application.IBeforeCommitDomainEventHandler`1");
        IAfterCommitDomainEventHandler = compilation.GetTypeByMetadataName("D3Fragments.Application.IAfterCommitDomainEventHandler`1");
        IPipelineBehavior = compilation.GetTypeByMetadataName("D3Fragments.Application.IPipelineBehavior`2");
        IUnitOfWork = compilation.GetTypeByMetadataName("D3Fragments.Application.IUnitOfWork");
        RepositoryBase = compilation.GetTypeByMetadataName("D3Fragments.EFCore.RepositoryBase`3");
        DbContext = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
        DbSet = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");
        DatabaseFacade = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade");
        IDbContextTransaction = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction");
        EntityFrameworkQueryableExtensions = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions");
        ControllerBase = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.ControllerBase");
        PageBase = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.RazorPages.PageBase");
        PageModel = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Mvc.RazorPages.PageModel");
        IComponent = compilation.GetTypeByMetadataName("Microsoft.AspNetCore.Components.IComponent");
        ICollection = compilation.GetSpecialType(SpecialType.System_Collections_Generic_ICollection_T);
        IDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2");
    }

    public INamedTypeSymbol? ArchitectureLayerAttribute { get; }
    public INamedTypeSymbol? EntityBase { get; }
    public INamedTypeSymbol? AggregateRoot { get; }
    public INamedTypeSymbol? IValueObject { get; }
    public INamedTypeSymbol? ValueObjectBase { get; }
    public INamedTypeSymbol? IRepository { get; }
    public INamedTypeSymbol? IDomainRule { get; }
    public INamedTypeSymbol? TypeSafeEnum { get; }
    public INamedTypeSymbol? IRequest { get; }
    public INamedTypeSymbol? ICommand { get; }
    public INamedTypeSymbol? IQuery { get; }
    public INamedTypeSymbol? IRequestHandler { get; }
    public INamedTypeSymbol? IRequestExecutor { get; }
    public INamedTypeSymbol? IBeforeCommitDomainEventHandler { get; }
    public INamedTypeSymbol? IAfterCommitDomainEventHandler { get; }
    public INamedTypeSymbol? IPipelineBehavior { get; }
    public INamedTypeSymbol? IUnitOfWork { get; }
    public INamedTypeSymbol? RepositoryBase { get; }
    public INamedTypeSymbol? DbContext { get; }
    public INamedTypeSymbol? DbSet { get; }
    public INamedTypeSymbol? DatabaseFacade { get; }
    public INamedTypeSymbol? IDbContextTransaction { get; }
    public INamedTypeSymbol? EntityFrameworkQueryableExtensions { get; }
    public INamedTypeSymbol? ControllerBase { get; }
    public INamedTypeSymbol? PageBase { get; }
    public INamedTypeSymbol? PageModel { get; }
    public INamedTypeSymbol? IComponent { get; }
    public INamedTypeSymbol ICollection { get; }
    public INamedTypeSymbol? IDictionary { get; }

    public static KnownTypes Create(Compilation compilation) => new(compilation);
}