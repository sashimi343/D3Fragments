# Quick start

This page explains the basics of D3Fragments by building a Web API that registers and retrieves books.
It uses SQLite as the database.

When finished, the solution consists of the following four projects.

| Project | Contents |
|---|---|
| `Library.Domain` | The `Book` aggregate, the `BookTitle` value object, and the repository interface |
| `Library.Application` | A Command for registration and a Query for retrieval |
| `Library.Infrastructure` | The `DbContext` and the repository implementation |
| `Library.Web` | Minimal API endpoints and DI registration |

## 1. Create the domain layer

Add `Sashimi343.D3Fragments` to `Library.Domain`.

First, create a value object that represents a book title.
A value object derives from `ValueObjectBase<TSelf, TPrimitive>` and implements `IValueObject<TSelf, TPrimitive>`.
Write the validation in `ValidateValue`. The base class constructor calls `ValidateValue` and throws a `DomainValidationException` if the value is invalid.

```csharp
using System.Diagnostics.CodeAnalysis;
using D3Fragments;

namespace Library.Domain.Books;

public sealed class BookTitle : ValueObjectBase<BookTitle, string>, IValueObject<BookTitle, string>
{
    private BookTitle(string value) : base(value)
    {
    }

    public static BookTitle Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > 200)
        {
            errorMessage = "The title must be between 1 and 200 characters.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
```

Next, create the `Book` aggregate root.
When you derive from `AggregateRoot`, an ID (`UuidV7`) is generated automatically, and the creation time, update time, and version number are recorded automatically.

```csharp
using D3Fragments;

namespace Library.Domain.Books;

public sealed class Book : AggregateRoot
{
    private Book(BookTitle title, string author)
    {
        Title = title;
        Author = author;
    }

    // For EF Core (value object properties cannot be bound to constructor parameters)
    private Book()
    {
        Title = null!;
        Author = null!;
    }

    public BookTitle Title { get; private set; }

    public string Author { get; private set; }

    public static Book Create(string title, string author) => new(BookTitle.Create(title), author);
}
```

Finally, define the repository interface.
By deriving from `IRepository<TAggregate>`, the basic operations such as `FindAsync`, `Add`, and `Delete` are already defined.

```csharp
using D3Fragments;

namespace Library.Domain.Books;

public interface IBookRepository : IRepository<Book>
{
}
```

## 2. Create the application layer

Add `Sashimi343.D3Fragments.Application` to `Library.Application` and reference `Library.Domain`.

Define operations that change state as Commands (`ICommand<TResponse>`) and read-only operations as Queries (`IQuery<TResponse>`), and implement a handler for each.
Handlers only work with repositories; they do not save anything.
When a Command handler completes successfully, the pipeline saves the changes.

```csharp
using D3Fragments;
using D3Fragments.Application;
using Library.Domain.Books;

namespace Library.Application.Books;

public sealed record RegisterBookCommand(string Title, string Author) : ICommand<UuidV7>;

public sealed class RegisterBookCommandHandler(IBookRepository books)
    : IRequestHandler<RegisterBookCommand, UuidV7>
{
    public Task<UuidV7> HandleAsync(RegisterBookCommand request, CancellationToken cancellationToken)
    {
        var book = Book.Create(request.Title, request.Author);
        books.Add(book);
        return Task.FromResult(book.Id);
    }
}
```

```csharp
using D3Fragments;
using D3Fragments.Application;
using Library.Domain.Books;

namespace Library.Application.Books;

public sealed record BookDto(UuidV7 Id, string Title, string Author);

public sealed record GetBookQuery(UuidV7 BookId) : IQuery<BookDto>;

public sealed class GetBookQueryHandler(IBookRepository books)
    : IRequestHandler<GetBookQuery, BookDto>
{
    public async Task<BookDto> HandleAsync(GetBookQuery request, CancellationToken cancellationToken)
    {
        var book = await books.FindAsync(request.BookId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Book));
        return new BookDto(book.Id, book.Title.Value, book.Author);
    }
}
```

Failures are expressed as exceptions.
A `DomainException` such as `EntityNotFoundException` is converted into an error response by the presentation layer.

## 3. Create the infrastructure layer

Add `Sashimi343.D3Fragments.EFCore` and `Microsoft.EntityFrameworkCore.Sqlite` to `Library.Infrastructure` and reference `Library.Application`.

The `DbContext` derives from `D3DbContext`.
Map value object properties with `ComplexValueObject`.
If you do not map them, an exception is thrown when the model is built.

```csharp
using D3Fragments.EFCore;
using Library.Domain.Books;
using Microsoft.EntityFrameworkCore;

namespace Library.Infrastructure;

public sealed class LibraryDbContext(DbContextOptions options) : D3DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(builder => builder.ComplexValueObject(x => x.Title));
    }
}
```

The repository implementation derives from `RepositoryBase<TContext, TAggregate>`.
The base class implements the basic operations, so in this example there is nothing else to write.

```csharp
using D3Fragments.EFCore;
using Library.Domain.Books;

namespace Library.Infrastructure;

public sealed class BookRepository(LibraryDbContext dbContext)
    : RepositoryBase<LibraryDbContext, Book>(dbContext), IBookRepository
{
}
```

## 4. Create the presentation layer

Add `Sashimi343.D3Fragments.AspNetCore` to `Library.Web` (an empty ASP.NET Core project) and reference `Library.Application` and `Library.Infrastructure`.

Register the D3Fragments services and define the endpoints in `Program.cs`.
Endpoints execute Commands and Queries through `IRequestExecutor<TRequest, TResponse>`.

```csharp
using D3Fragments;
using D3Fragments.Application;
using D3Fragments.AspNetCore;
using D3Fragments.EFCore;
using Library.Application.Books;
using Library.Domain.Books;
using Library.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register handlers and the pipeline
builder.Services.AddD3FragmentsApplication([typeof(RegisterBookCommand).Assembly]);

// Register the DbContext, the Unit of Work, and repositories
builder.Services.AddD3FragmentsEFCore<LibraryDbContext>(
    [typeof(Book).Assembly, typeof(BookRepository).Assembly],
    options => options.UseSqlite("Data Source=library.db"));

// Convert DomainException into Problem Details responses
builder.Services.AddD3FragmentsAspNetCore();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<LibraryDbContext>().Database.EnsureCreatedAsync();
}

app.UseExceptionHandler();

app.MapPost("/books", async (
    RegisterBookCommand command,
    IRequestExecutor<RegisterBookCommand, UuidV7> executor,
    CancellationToken cancellationToken) =>
{
    var id = await executor.ExecuteAsync(command, cancellationToken);
    return TypedResults.Created($"/books/{id}", new { id });
});

app.MapGet("/books/{id}", (
    UuidV7 id,
    IRequestExecutor<GetBookQuery, BookDto> executor,
    CancellationToken cancellationToken) =>
    executor.ExecuteAsync(new GetBookQuery(id), cancellationToken));

app.Run();
```

Handlers are registered automatically from the assemblies passed to `AddD3FragmentsApplication`, and repositories from the assemblies passed to `AddD3FragmentsEFCore`.
The assemblies passed to `AddD3FragmentsEFCore` are also used to register value converters for type-safe enums, so pass the domain layer assembly as well.

## 5. Try it out

Start the application and register a book.

```bash
curl -i -X POST http://localhost:5000/books -H "Content-Type: application/json" -d '{"title":"Domain-Driven Design","author":"Eric Evans"}'
```

`201 Created` is returned with the generated ID.
Calling `GET /books/{id}` with the returned ID retrieves the registered book.
If you send an empty title, the validation in `ValidateValue` fails and a `400 Bad Request` Problem Details response is returned.
Retrieving an ID that does not exist returns `404 Not Found`.

## Next steps

For a larger example, see the [demo application](https://github.com/sashimi343/D3Fragments/tree/main/demo/01_BasicWebApp) (library lending management).
It also implements aggregates with child entities, domain events, domain rules, and error display in Razor Pages.

- [Architecture](../guide/01-architecture.md): how to split layers and the dependency rules
- [Domain layer](../guide/03-domain/index.md): building blocks such as entities and value objects
- [Application layer](../guide/04-application/index.md): Commands, Queries, and the pipeline
