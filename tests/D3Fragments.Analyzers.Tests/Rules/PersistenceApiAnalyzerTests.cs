using D3Fragments.Analyzers.Tests.Helpers;
using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.PersistenceApiAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class PersistenceApiAnalyzerTests
{
    private const ArchitectureLayerType All =
        ArchitectureLayerType.Presentation | ArchitectureLayerType.Application | ArchitectureLayerType.Domain | ArchitectureLayerType.Infrastructure;

    // 各モジュールレイヤーの型。テストごとに参照元の型を追加する
    private const string Types = """
        using System.Collections.Generic;
        using System.Linq;
        using System.Threading;
        using System.Threading.Tasks;
        using D3Fragments;
        using D3Fragments.Application;
        using D3Fragments.EFCore;
        using Microsoft.EntityFrameworkCore;

        public sealed class Shelf : AggregateRoot
        {
            public void Update() { }
        }
        public sealed class AuditLog
        {
            public int Id { get; set; }
        }
        public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
        {
            public DbSet<Shelf> Shelves => Set<Shelf>();
            public DbSet<AuditLog> Logs => Set<AuditLog>();
        }
        public interface IShelfRepository : IRepository<Shelf> { }
        public sealed record AddShelf : ICommand<int>;
        """;

    private static Microsoft.CodeAnalysis.Testing.DiagnosticResult Expect(Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor, int location, params object[] arguments) =>
        Verifier.Diagnostic(descriptor).WithLocation(location).WithArguments(arguments);

    [Fact]
    public async Task HandlerSavingThroughUnitOfWork_ReportsDfr0012()
    {
        const string source = Types + """
            public sealed class AddShelfHandler(IUnitOfWork unitOfWork) : IRequestHandler<AddShelf, int>
            {
                public async Task<int> HandleAsync(AddShelf request, CancellationToken cancellationToken)
                {
                    await {|#0:unitOfWork.SaveChangesAsync(cancellationToken)|};
                    return 0;
                }
            }
            """;

        await Verifier.VerifyAsync(source, All, Expect(DiagnosticDescriptors.DFR0012, 0, "AddShelfHandler", "IUnitOfWork.SaveChangesAsync"));
    }

    [Fact]
    public async Task RepositorySavingOrControllingTransactions_ReportsDfr0012()
    {
        const string source = Types + """
            public sealed class ShelfRepository(AppDbContext dbContext) : RepositoryBase<AppDbContext, Shelf>(dbContext), IShelfRepository
            {
                public async Task SaveAsync(CancellationToken cancellationToken)
                {
                    await using var transaction = await {|#0:dbContext.Database.BeginTransactionAsync(cancellationToken)|};
                    await {|#1:dbContext.SaveChangesAsync(cancellationToken)|};
                    await {|#2:transaction.CommitAsync(cancellationToken)|};
                }
            }
            """;

        await Verifier.VerifyAsync(source, All,
            Expect(DiagnosticDescriptors.DFR0012, 0, "ShelfRepository", "DatabaseFacade.BeginTransactionAsync"),
            Expect(DiagnosticDescriptors.DFR0012, 1, "ShelfRepository", "DbContext.SaveChangesAsync"),
            Expect(DiagnosticDescriptors.DFR0012, 2, "ShelfRepository", "IDbContextTransaction.CommitAsync"));
    }

    [Fact]
    public async Task PipelineBehaviorDbContextAndUnitOfWork_MayControlPersistence()
    {
        const string source = Types + """
            public sealed class AuditBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
                where TRequest : IRequest<TResponse>
            {
                public int Order => 400;

                public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
                {
                    var response = await next(cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                    return response;
                }
            }
            public sealed class AuditedDbContext(DbContextOptions<AuditedDbContext> options) : DbContext(options)
            {
                public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
                    base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
            public sealed class CustomUnitOfWork(AppDbContext dbContext) : IUnitOfWork
            {
                public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

                public void DiscardChanges() => dbContext.ChangeTracker.Clear();

                public async Task<TResponse> ExecuteInTransactionAsync<TResponse>(System.Func<CancellationToken, Task<TResponse>> action, CancellationToken cancellationToken)
                {
                    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                    var response = await action(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return response;
                }
            }
            """;

        await Verifier.VerifyAsync(source, All);
    }

    [Fact]
    public async Task UpdateOrAttachForEntity_ReportsDfr0013()
    {
        const string source = Types + """
            public sealed class ShelfRepository(AppDbContext dbContext) : RepositoryBase<AppDbContext, Shelf>(dbContext), IShelfRepository
            {
                public void Store(Shelf shelf, List<Shelf> shelves)
                {
                    {|#0:dbContext.Update(shelf)|};
                    {|#1:dbContext.Shelves.Update(shelf)|};
                    {|#2:dbContext.Attach(shelf)|};
                    {|#3:dbContext.Shelves.AttachRange(shelves)|};
                    {|#4:dbContext.UpdateRange(shelf, shelf)|};
                }
            }
            """;

        await Verifier.VerifyAsync(source, All,
            Expect(DiagnosticDescriptors.DFR0013, 0, "DbContext.Update", "Shelf"),
            Expect(DiagnosticDescriptors.DFR0013, 1, "DbSet.Update", "Shelf"),
            Expect(DiagnosticDescriptors.DFR0013, 2, "DbContext.Attach", "Shelf"),
            Expect(DiagnosticDescriptors.DFR0013, 3, "DbSet.AttachRange", "Shelf"),
            Expect(DiagnosticDescriptors.DFR0013, 4, "DbContext.UpdateRange", "Shelf"));
    }

    // エンティティ以外の型（キーをDB側で採番するテーブル等）と、同名のドメインメソッドは対象外
    [Fact]
    public async Task UpdateForNonEntityOrDomainMethod_ReportsNothing()
    {
        const string source = Types + """
            public sealed class AuditWriter(AppDbContext dbContext)
            {
                public void Write(AuditLog log, Shelf shelf)
                {
                    dbContext.Update(log);
                    dbContext.Logs.Update(log);
                    shelf.Update();
                }
            }
            """;

        await Verifier.VerifyAsync(source, All);
    }

    [Fact]
    public async Task RepositoryDisablingTracking_ReportsDfr0014()
    {
        const string source = Types + """
            public sealed class ShelfRepository(AppDbContext dbContext) : RepositoryBase<AppDbContext, Shelf>(dbContext), IShelfRepository
            {
                public Task<List<Shelf>> ListAsync(CancellationToken cancellationToken) =>
                    {|#0:Set.AsNoTracking()|}.ToListAsync(cancellationToken);

                public Task<List<Shelf>> ListIdentityResolvedAsync(CancellationToken cancellationToken) =>
                    {|#1:Set.AsTracking(QueryTrackingBehavior.NoTrackingWithIdentityResolution)|}.ToListAsync(cancellationToken);

                public Task<List<Shelf>> ListTrackedAsync(CancellationToken cancellationToken) =>
                    Set.AsTracking().AsTracking(QueryTrackingBehavior.TrackAll).ToListAsync(cancellationToken);
            }
            """;

        await Verifier.VerifyAsync(source, All,
            Expect(DiagnosticDescriptors.DFR0014, 0, "ShelfRepository", "AsNoTracking"),
            Expect(DiagnosticDescriptors.DFR0014, 1, "ShelfRepository", "AsTracking"));
    }

    [Fact]
    public async Task QueryServiceDisablingTracking_ReportsNothing()
    {
        const string source = Types + """
            public sealed class ShelfQueryService(AppDbContext dbContext)
            {
                public Task<int> CountAsync(CancellationToken cancellationToken) => dbContext.Shelves.AsNoTracking().CountAsync(cancellationToken);
            }
            """;

        await Verifier.VerifyAsync(source, All);
    }

    [Fact]
    public async Task AssemblyWithoutAttribute_ReportsNothing()
    {
        const string source = Types + """
            public sealed class Seeder(AppDbContext dbContext)
            {
                public Task SeedAsync(Shelf shelf)
                {
                    dbContext.Update(shelf);
                    return dbContext.SaveChangesAsync();
                }
            }
            """;

        await Verifier.VerifyAsync(source);
    }
}
