using Verifier = D3Fragments.Analyzers.Tests.Helpers.AnalyzerVerifier<D3Fragments.Analyzers.Rules.RepositorySignatureAnalyzer>;

namespace D3Fragments.Analyzers.Tests.Rules;

public sealed class RepositorySignatureAnalyzerTests
{
    private const string Types = """
        using System;
        using System.Collections.Generic;
        using System.Threading;
        using System.Threading.Tasks;
        using D3Fragments;

        public class Bookshelf : AggregateRoot { }
        public sealed class SpecialBookshelf : Bookshelf { }
        public sealed class Book : Entity { }
        public sealed class User : AggregateRoot { }

        """;

    [Fact]
    public async Task MethodReturningPartOfAggregate_ReportsDfr0004WithMessage()
    {
        const string source = Types + """
            public interface IBookshelfRepository : IRepository<Bookshelf>
            {
                Task<IReadOnlyList<Book>> {|#0:FindBooksAsync|}(UuidV7 bookshelfId, CancellationToken cancellationToken);
            }
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0004)
            .WithLocation(0)
            .WithArguments("IBookshelfRepository.FindBooksAsync(UuidV7, CancellationToken)", "Bookshelf", "Book"));
    }

    [Fact]
    public async Task ReturnTypes_AreInspectedRecursively()
    {
        const string source = Types + """
            public interface IBookshelfRepository : IRepository<Bookshelf, UuidV7>
            {
                Book {|DFR0004:FindBook|}();
                Book[] {|DFR0004:FindBookArray|}();
                ValueTask<Dictionary<UuidV7, List<Book>>> {|DFR0004:FindBookMap|}();
                IAsyncEnumerable<Book> {|DFR0004:StreamBooks|}();
                (Bookshelf Shelf, Book Book) {|DFR0004:FindPair|}();
                Task<Result<Book>> {|DFR0004:TryFindBook|}();
                Task<User?> {|DFR0004:FindOwner|}();

                Task<Bookshelf?> FindByNameAsync(string name);
                Task<IReadOnlyList<Bookshelf>> FindAllAsync();
                Task<SpecialBookshelf?> FindSpecialAsync();
                Task<int> CountBooksAsync();
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task MethodReceivingPartOfAggregate_ReportsDfr0005WithMessage()
    {
        const string source = Types + """
            public interface IBookshelfRepository : IRepository<Bookshelf>
            {
                void UpdateBook(Book {|#0:book|});
            }
            """;

        await Verifier.VerifyAsync(source, Verifier.Diagnostic(DiagnosticDescriptors.DFR0005)
            .WithLocation(0)
            .WithArguments("IBookshelfRepository.UpdateBook(Book)", "book", "Bookshelf", "Book"));
    }

    [Fact]
    public async Task ParameterTypes_AreInspectedRecursively()
    {
        const string source = Types + """
            public interface IBookshelfRepository : IRepository<Bookshelf>
            {
                Task<Bookshelf?> FindByBookAsync(Book {|DFR0005:book|}, CancellationToken cancellationToken);
                void UpdateBooks(IEnumerable<Book> {|DFR0005:books|}, params Book[] {|DFR0005:others|});
                bool TryFind(UuidV7 id, out Book? {|DFR0005:book|});
                void Save(Bookshelf bookshelf, SpecialBookshelf special, UuidV7 bookId);
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task InheritedRepositoryInterface_IsInspected()
    {
        const string source = Types + """
            public interface IBookshelfRepository : IRepository<Bookshelf> { }
            public interface IExtendedBookshelfRepository : IBookshelfRepository
            {
                Task<Book?> {|DFR0004:FindBookAsync|}(UuidV7 id);
            }
            """;

        await Verifier.VerifyAsync(source);
    }

    [Fact]
    public async Task NonRepositoryInterfaceAndImplementation_ReportNothing()
    {
        const string source = Types + """
            public interface IBookService
            {
                Book FindBook(Book book);
            }

            public abstract class BookshelfRepository : IRepository<Bookshelf>
            {
                public abstract Task<Bookshelf?> FindAsync(UuidV7 id, CancellationToken cancellationToken);
                public abstract void Add(Bookshelf aggregate);
                public abstract void Delete(DeletionToken<Bookshelf> token);
                public abstract Task<int> CountAsync(CancellationToken cancellationToken);
                public abstract Task<bool> ExistsAsync(UuidV7 id, CancellationToken cancellationToken);
                public Book? FindBook(Book book) => book;
            }

            // 集約ルートが型パラメーターの汎用インターフェース
            public interface IReadRepository<T> : IRepository<T> where T : AggregateRoot
            {
                Task<Book?> FindBookAsync();
            }
            """;

        await Verifier.VerifyAsync(source);
    }
}
