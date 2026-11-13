using D3Fragments.Analyzers.Common;
using D3Fragments.Analyzers.Tests.Helpers;

using Microsoft.CodeAnalysis;

namespace D3Fragments.Analyzers.Tests.Common;

public sealed class ModuleLayerResolverTests
{
    private const string Source = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;
        using D3Fragments;
        using D3Fragments.Application;
        using Microsoft.AspNetCore.Mvc;
        using Microsoft.EntityFrameworkCore;

        public sealed class BooksController : ControllerBase { }
        public abstract class BooksPage : Microsoft.AspNetCore.Mvc.RazorPages.PageModel { }
        public sealed class BooksIndex : BooksPage { }
        public sealed record GetShelf(UuidV7 Id) : IQuery<int>;
        public sealed class GetShelfHandler : IRequestHandler<GetShelf, int>
        {
            public Task<int> HandleAsync(GetShelf request, CancellationToken cancellationToken) => Task.FromResult(0);
        }
        public sealed record ShelfCreated(DateTimeOffset OccurredAt) : IDomainEvent;
        public sealed class ShelfCreatedHandler : IBeforeCommitDomainEventHandler<ShelfCreated>
        {
            public Task HandleAsync(ShelfCreated domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
        }
        public sealed class Shelf : AggregateRoot { }
        public sealed class ShelfName : ValueObjectBase<ShelfName, string>, IValueObject<ShelfName, string>
        {
            private ShelfName(string value) : base(value) { }
            public static ShelfName Create(string value) => new(value);
            public static bool ValidateValue(string value, out string? errorMessage) { errorMessage = null; return true; }
        }
        public interface IShelfRepository : IRepository<Shelf> { }
        public sealed class ShelfRule : IDomainRule<Shelf>
        {
            public Result Check(Shelf context) => Result.Success();
        }
        public abstract class ShelfRepository : IShelfRepository
        {
            public abstract Task<Shelf?> FindAsync(UuidV7 id, CancellationToken cancellationToken);
            public abstract void Add(Shelf aggregate);
            public abstract void Delete(DeletionToken<Shelf> token);
            public abstract Task<int> CountAsync(CancellationToken cancellationToken);
            public abstract Task<bool> ExistsAsync(UuidV7 id, CancellationToken cancellationToken);
        }
        public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
            where TRequest : IRequest<TResponse>
        {
            public int Order => 0;
            public Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) => next(cancellationToken);
        }
        public sealed class AppDbContext(DbContextOptions options) : DbContext(options) { }
        public sealed class AppUnitOfWork : IUnitOfWork
        {
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
            public void DiscardChanges() { }
            public Task<TResponse> ExecuteInTransactionAsync<TResponse>(Func<CancellationToken, Task<TResponse>> action, CancellationToken cancellationToken) => action(cancellationToken);
        }
        public sealed class Helper { }

        // 複数のモジュールレイヤーの条件に該当する（記載順で Application_DomainEventHandler が優先される）
        public sealed class EntityAndHandler : Entity, IAfterCommitDomainEventHandler<ShelfCreated>
        {
            public Task HandleAsync(ShelfCreated domainEvent, CancellationToken cancellationToken) => Task.CompletedTask;
        }
        """;

    private const ArchitectureLayerType All =
        ArchitectureLayerType.Presentation | ArchitectureLayerType.Application | ArchitectureLayerType.Domain | ArchitectureLayerType.Infrastructure;

    [Theory]
    [InlineData("BooksController", ArchitectureLayerType.Presentation, "Presentation_Controller")]
    [InlineData("BooksPage", ArchitectureLayerType.Presentation, "Presentation_Controller")]
    [InlineData("BooksIndex", ArchitectureLayerType.Presentation, "Presentation_Controller")]
    [InlineData("Helper", ArchitectureLayerType.Presentation, "Presentation_Other")]
    [InlineData("GetShelf", ArchitectureLayerType.Application, "Application_Request")]
    [InlineData("GetShelfHandler", ArchitectureLayerType.Application, "Application_RequestHandler")]
    [InlineData("ShelfCreatedHandler", ArchitectureLayerType.Application, "Application_DomainEventHandler")]
    [InlineData("LoggingBehavior`2", ArchitectureLayerType.Application, "Application_PipelineBehavior")]
    [InlineData("Helper", ArchitectureLayerType.Application, "Application_Other")]
    [InlineData("Shelf", ArchitectureLayerType.Domain, "Domain_Entity")]
    [InlineData("ShelfName", ArchitectureLayerType.Domain, "Domain_ValueObject")]
    [InlineData("IShelfRepository", ArchitectureLayerType.Domain, "Domain_RepositoryInterface")]
    [InlineData("ShelfRule", ArchitectureLayerType.Domain, "Domain_DomainRule")]
    [InlineData("Helper", ArchitectureLayerType.Domain, "Domain_Other")]
    [InlineData("ShelfRepository", ArchitectureLayerType.Infrastructure, "Infrastructure_RepositoryImplementation")]
    [InlineData("AppDbContext", ArchitectureLayerType.Infrastructure, "Infrastructure_DbContext")]
    [InlineData("AppUnitOfWork", ArchitectureLayerType.Infrastructure, "Infrastructure_UnitOfWork")]
    [InlineData("Helper", ArchitectureLayerType.Infrastructure, "Infrastructure_Other")]
    public void Resolve_SingleLayerAssembly_ReturnsModuleLayerOfThatLayer(string typeName, ArchitectureLayerType layer, string expected) =>
        Assert.Equal(expected, Resolve(TestCompilation.Create(Source, layer), typeName));

    [Theory]
    [InlineData("BooksController", ArchitectureLayerType.Domain, "Domain_Other")]
    [InlineData("Shelf", ArchitectureLayerType.Application, "Application_Other")]
    [InlineData("ShelfRepository", ArchitectureLayerType.Domain, "Domain_Other")]
    public void Resolve_TypeOfOtherLayer_ReturnsOtherOfAssemblyLayer(string typeName, ArchitectureLayerType layer, string expected) =>
        Assert.Equal(expected, Resolve(TestCompilation.Create(Source, layer), typeName));

    [Theory]
    [InlineData("Shelf", "Domain_Entity")]
    [InlineData("GetShelfHandler", "Application_RequestHandler")]
    [InlineData("BooksController", "Other")]
    [InlineData("Helper", "Other")]
    public void Resolve_MultiLayerAssembly_ReturnsModuleLayerOfAnyLayerOrOther(string typeName, string expected) =>
        Assert.Equal(expected, Resolve(TestCompilation.Create(Source, ArchitectureLayerType.Application | ArchitectureLayerType.Domain), typeName));

    [Fact]
    public void Resolve_TypeMatchingMultipleModuleLayers_ReturnsFirstInDocumentedOrder() =>
        Assert.Equal("Application_DomainEventHandler", Resolve(TestCompilation.Create(Source, All), "EntityAndHandler"));

    [Fact]
    public void Resolve_AssemblyWithoutAttribute_ReturnsNull() =>
        Assert.Null(Resolve(TestCompilation.Create(Source), "Shelf"));

    [Fact]
    public void Resolve_TypeInReferencedAssembly_UsesAttributeOfThatAssembly()
    {
        var domain = TestCompilation.Create(Source, ArchitectureLayerType.Domain, "DomainAssembly");
        var presentation = TestCompilation.Create(
            "public sealed class Dummy { }", ArchitectureLayerType.Presentation, "PresentationAssembly", domain.ToMetadataReference());

        Assert.Equal("Domain_RepositoryInterface", Resolve(presentation, "IShelfRepository"));
    }

    [Fact]
    public void Resolve_ConstructedGenericType_ResolvesOriginalDefinition()
    {
        var compilation = TestCompilation.Create(
            "public interface IShelfRepository<T> : D3Fragments.IRepository<Shelf> { } public sealed class Shelf : D3Fragments.AggregateRoot { }",
            ArchitectureLayerType.Domain);
        var constructed = compilation.GetType("IShelfRepository`1").Construct(compilation.GetSpecialType(SpecialType.System_Int32));

        Assert.Equal("Domain_RepositoryInterface", CreateResolver(compilation).Resolve(constructed)?.ToString());
    }

    private static string? Resolve(Compilation compilation, string typeName) =>
        CreateResolver(compilation).Resolve(compilation.GetType(typeName))?.ToString();

    private static ModuleLayerResolver CreateResolver(Compilation compilation)
    {
        var knownTypes = KnownTypes.Create(compilation);
        return new ModuleLayerResolver(knownTypes, new ArchitectureLayerResolver(knownTypes));
    }
}