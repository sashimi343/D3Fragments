using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.RepositoryFindAsyncAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class RepositoryFindAsyncAnalyzerTests
{
    private const string Types = """
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using D3Fragments;
        using D3Fragments.EFCore;
        using Microsoft.EntityFrameworkCore;

        public sealed class AppDbContext(DbContextOptions options) : DbContext(options) { }
        public sealed class Book : Entity { }
        public sealed class User : AggregateRoot { }
        public class Bookshelf : AggregateRoot
        {
            public IReadOnlyList<Book> Books { get; } = [];
        }
        public sealed class Tag : AggregateRoot
        {
            public string Name { get; } = "";
            public UuidV7 OwnerId { get; }
        }
        public sealed class Favorite : AggregateRoot
        {
            public User? Owner { get; }
        }

        """;

    [Fact]
    public async Task RepositoryWithoutOverride_ReportsDfr0015WithMessage()
    {
        const string source = Types + """
            public sealed class {|#0:BookshelfRepository|}(AppDbContext dbContext) : RepositoryBase<AppDbContext, Bookshelf>(dbContext);
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0015)
            .WithLocation(0)
            .WithArguments("BookshelfRepository", "Bookshelf", "Bookshelf.Books"));
    }

    [Fact]
    public async Task ChildEntitiesInBaseClassAndArrays_AreDetected()
    {
        const string source = Types + """
            public sealed class SpecialBookshelf : Bookshelf { }
            public sealed class Library : AggregateRoot
            {
                public Book[] Books { get; } = [];
            }
            public sealed class {|DFR0015:SpecialBookshelfRepository|}(AppDbContext dbContext) : RepositoryBase<AppDbContext, SpecialBookshelf>(dbContext);
            public sealed class {|DFR0015:LibraryRepository|}(AppDbContext dbContext) : RepositoryBase<AppDbContext, Library>(dbContext);
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task OverriddenFindAsync_DoesNotReport()
    {
        const string source = Types + """
            public abstract class BookshelfRepositoryBase(AppDbContext dbContext) : RepositoryBase<AppDbContext, Bookshelf>(dbContext)
            {
                public override Task<Bookshelf?> FindAsync(UuidV7 id, CancellationToken cancellationToken) =>
                    Set.Include(x => x.Books).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            }
            public sealed class BookshelfRepository(AppDbContext dbContext) : RepositoryBase<AppDbContext, Bookshelf>(dbContext)
            {
                public override Task<Bookshelf?> FindAsync(UuidV7 id, CancellationToken cancellationToken) =>
                    Set.Include(x => x.Books).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
            }
            public sealed class DerivedBookshelfRepository(AppDbContext dbContext) : BookshelfRepositoryBase(dbContext);
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task AggregateWithoutChildEntities_DoesNotReport()
    {
        const string source = Types + """
            public sealed class TagRepository(AppDbContext dbContext) : RepositoryBase<AppDbContext, Tag>(dbContext);
            public sealed class FavoriteRepository(AppDbContext dbContext) : RepositoryBase<AppDbContext, Favorite>(dbContext);
            public abstract class AbstractBookshelfRepository(AppDbContext dbContext) : RepositoryBase<AppDbContext, Bookshelf>(dbContext);
            """;

        await Verifier.VerifyAsync(source);
    }
}
