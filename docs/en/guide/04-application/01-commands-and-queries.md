# Commands and Queries

A use case is implemented as a pair of a request and a handler.

- Command (`ICommand<TResponse>`): a request that changes state. When the handler completes successfully, the changes are saved.
- Query (`IQuery<TResponse>`): a read-only request. Changes are not saved.

## Defining a request

A request implements exactly one of `ICommand<TResponse>` and `IQuery<TResponse>`.
`TResponse` is the type of the value returned on success.

```csharp
public sealed class RegisterBookCommand : ICommand<Book>
{
    public required string Isbn { get; init; }

    public required string Title { get; init; }

    public required string Author { get; init; }
}

public sealed record GetBookQuery(UuidV7 BookId) : IQuery<BookDto>;
```

Do not implement `IRequest<TResponse>` directly.
D3Fragments decides whether to save changes based on whether the request type is an `IQuery`.
A request that cannot be identified as either causes an exception when its handler is registered, and the analyzers also warn about it ([DFR0016](../../analyzers/DFR0016.md)).

## Implementing a handler

Implement `IRequestHandler<TRequest, TResponse>`.
A handler gets aggregates from repositories and calls operations on them.

```csharp
public sealed class DeleteBookCommandHandler(IBookRepository books)
    : IRequestHandler<DeleteBookCommand, Book>
{
    public async Task<Book> HandleAsync(DeleteBookCommand request, CancellationToken cancellationToken)
    {
        Book book = await books.FindAsync(request.BookId, cancellationToken)
            ?? throw new EntityNotFoundException(nameof(Book));
        books.Delete(book.IssueDeletionToken());
        return book;
    }
}
```

Handlers follow these rules:

- Do not save: a handler does not call `SaveChangesAsync` or control transactions. Pipeline behaviors take care of saving and transactions ([DFR0012](../../analyzers/DFR0012.md)).
- Express failures as exceptions: on failure, throw a `DomainException`. There is no need to include failures in the response type (for example, by returning a `Result`). The pipeline decides whether to save the changes based only on whether an exception was thrown.

## Executing a request

Execute requests through `IRequestExecutor<TRequest, TResponse>`.
If you call a handler directly, the pipeline behaviors do not run and the changes are not saved.

```csharp
[ApiController]
[Route("api/holds")]
public sealed class HoldsController(IRequestExecutor<ExpireHoldsCommand, int> expireHolds) : ControllerBase
{
    [HttpPost("expire")]
    public async Task<IActionResult> ExpireAsync(CancellationToken cancellationToken)
    {
        int processedCount = await expireHolds.ExecuteAsync(new ExpireHoldsCommand(), cancellationToken);
        return Ok(new { processedCount });
    }
}
```

In the presentation layer, such as controllers, inject `IRequestExecutor` instead of domain layer classes through the constructor ([DFR0011](../../analyzers/DFR0011.md)).

### Receiving failures as a Result

With the `TryExecuteAsync` extension method, you can receive a `DomainException` thrown by a handler as a failed `Result<TResponse>`.
You do not have to write a `try-catch`, and you cannot accidentally swallow exceptions other than `DomainException`.

```csharp
Result<Book> result = await registerBook.TryExecuteAsync(command, cancellationToken);
if (result.IsFailure)
{
    ModelState.AddDomainErrors(result.Errors);
    return Page();
}
```

If you use the ASP.NET Core integration, you can also let the `DomainException` propagate and have it converted into an error response (see [Error responses](../06-aspnetcore/01-error-responses.md)).

## Related APIs

- <xref:D3Fragments.Application.ICommand`1>
- <xref:D3Fragments.Application.IQuery`1>
- <xref:D3Fragments.Application.IRequestHandler`2>
- <xref:D3Fragments.Application.IRequestExecutor`2>
- @D3Fragments.Application.RequestExecutorExtensions
