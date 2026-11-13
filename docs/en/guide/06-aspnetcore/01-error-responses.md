# Error responses

D3Fragments converts domain errors into responses in the [Problem Details](https://www.rfc-editor.org/rfc/rfc9457) (RFC 9457) format.
It converts the following two:

- A `DomainException` thrown by a handler or other code
- A failed `Result` / `Result<T>`

## Converting DomainException

When you call `AddD3FragmentsAspNetCore` and enable `app.UseExceptionHandler()`, a `DomainException` thrown out of an endpoint is converted into an error response.
Endpoints do not need to catch the exception.

```csharp
app.MapGet("/books/{id}", (UuidV7 id, IRequestExecutor<GetBookQuery, BookDto> executor, CancellationToken cancellationToken) =>
    executor.ExecuteAsync(new GetBookQuery(id), cancellationToken));
```

When the handler throws an `EntityNotFoundException`, the response looks like this:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "NotFound",
  "status": 404,
  "detail": "The specified Book is not found.",
  "traceId": "00-..."
}
```

| Member | Contents |
|---|---|
| `status` | The HTTP status code for the `ErrorType` of the first error |
| `title` | The name of the `ErrorType` of the first error |
| `detail` | The messages of all errors, joined with line breaks |
| `errorCode` | The error code (`DomainException.ErrorCode`). Omitted if there is no error code |

The response is written through `IProblemDetailsService`, so you can also add members with `CustomizeProblemDetails` in `AddProblemDetails`.

## Converting Result

In Minimal APIs, you can convert a `Result` into an `IResult` with `ToHttpResult`.

| Method | Response on success |
|---|---|
| `Result.ToHttpResult()` | 204 No Content |
| `Result<T>.ToHttpResult()` | 200 OK (with the value in the body) |
| `Result<T>.ToHttpResult(onSuccess)` | The response returned by `onSuccess` |

On failure, the error response has the same format as for `DomainException`.
`errorCode` contains the `Error.Code` of the first error.

```csharp
app.MapPost("/books", async (RegisterBookRequest body, IRequestExecutor<RegisterBookCommand, Book> executor, CancellationToken cancellationToken) =>
{
    Result<Book> result = await executor.TryExecuteAsync(body.ToCommand(), cancellationToken);
    return result.ToHttpResult(book => TypedResults.Created($"/books/{book.Id}", BookDto.From(book)));
});
```

## Changing status codes and error codes

Configure `DomainProblemDetailsOptions` with the delegate passed to `AddD3FragmentsAspNetCore`.

```csharp
builder.Services.AddD3FragmentsAspNetCore(options =>
{
    // Use 422 instead of 409 for InvalidState
    options.StatusCodeMap[ErrorType.InvalidState] = StatusCodes.Status422UnprocessableEntity;

    // Change the name of the member that holds the error code
    options.ErrorCodeKey = "code";
});
```

| Property | Description | Default |
|---|---|---|
| `StatusCodeMap` | A map from `ErrorType` to HTTP status codes. An `ErrorType` not in the map becomes 400 | See the table in [Result and errors](../03-domain/04-result-and-errors.md) |
| `ErrorCodeKey` | The name of the member that holds the error code | `errorCode` |

`GetStatusCode(errorType)` gets the HTTP status code from the map.
Use it when you build the response yourself, for example in Razor Pages.

## Related APIs

- @D3Fragments.AspNetCore.D3FragmentsServiceCollectionExtensions
- @D3Fragments.AspNetCore.ResultHttpExtensions
- @D3Fragments.AspNetCore.DomainProblemDetailsOptions
