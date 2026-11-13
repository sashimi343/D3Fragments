# Result and errors

D3Fragments expresses failures in the following two ways.

- `Result` / `Result<T>`: returns a failure as a return value. Use it, for example, when you want to return several validation errors together.
- `DomainException`: reports a failure as an exception. Command and Query handlers express failures as exceptions.

Both describe the failure with `Error`.
Therefore, `Result` and `DomainException` can be converted into each other.

## Error and ErrorType

`Error` is a class that holds the information of a single error.

| Property | Description |
|---|---|
| `Type` | The category of the error (`ErrorType`) |
| `Message` | The error message |
| `Code` | An application-specific error code (optional). Use it, for example, to include an error code in API responses |
| `Target` | A key that identifies what the error is about (optional). Use it, for example, to associate an error with an input field |

Create an `Error` with the factory method for its category.

```csharp
var error = Error.Validation("Specify a title.", target: "title");
```

The `ErrorType` values and their default HTTP status codes in the ASP.NET Core integration are as follows.

| `ErrorType` | Meaning | HTTP status |
|---|---|---|
| `Unauthenticated` | Not authenticated (the user cannot be identified) | 401 |
| `Validation` | The input is not valid | 400 |
| `NotFound` | The target does not exist | 404 |
| `Conflict` | A state conflict (such as a duplicate key) | 409 |
| `Forbidden` | No permission (the user is identified) | 403 |
| `InvalidState` | The entity or aggregate is in a state that does not accept the operation | 409 |
| `PreconditionFailed` | A precondition is not met | 422 |

## Result

`Result` represents success or failure, and holds a list of errors (`Errors`) on failure.
`Result<T>` also holds a value (`Value`) on success.

```csharp
Result success = Result.Success();
Result failure = Result.Failure(Error.PreconditionFailed("Loans are suspended for this member."));

Result<BookTitle> title = BookTitle.TryCreate(input);
if (title.IsSuccess)
{
    Use(title.Value); // Value is not null when IsSuccess is true
}
```

In the branch where `IsSuccess` is `true`, using `Value` does not produce a nullable warning.
You can also assign a value of `T` directly to `Result<T>` (implicit conversion).

### Combining multiple results

| Method | Description |
|---|---|
| `Result.Combine(results)` | Returns success if all succeeded; otherwise, returns a failure with all the errors |
| `Result.CombineErrors(results)` | Collects the errors of all the results into a single list |
| `result.Merge(other)` | Combines two results |

### Converting to an exception

There are methods that throw a `DomainException` when a `Result` is a failure.
They are useful in a handler that uses domain methods or domain rules that return `Result`.

```csharp
// On failure, throws a DomainException with all the errors
rule.Check(context).ThrowIfFailure();

// On success, returns the value; on failure, throws a DomainException
Book book = Book.TryCreate(isbn, title, author).GetValueOrThrow();
```

Both have an overload that takes an error code and an overload that lets you create the exception to throw.

## DomainException

`DomainException` is an exception that indicates a violation of a domain rule.
Create it from an `Error`, or by specifying a category and a message.

```csharp
throw new DomainException(Error.InvalidState("This book cannot be lent."));
throw new DomainException(ErrorType.InvalidState, "A book that is not on the shelf cannot be removed.");
```

You can also create it from multiple errors.
All the errors are available in `Errors`, and `ErrorType` and `Message` contain the values of the first error.

Commonly used categories have dedicated derived classes.

| Exception | `ErrorType` |
|---|---|
| `DomainValidationException` | `Validation` |
| `EntityNotFoundException` | `NotFound` |
| `ConflictOccurredException` | `Conflict` |

```csharp
var book = await books.FindAsync(id, cancellationToken)
    ?? throw new EntityNotFoundException(nameof(Book));
```

If you pass an entity name to `EntityNotFoundException`, the default message includes it.

### Converting an exception to a Result

With the `TryExecuteAsync` extension method on `IRequestExecutor`, you can receive a `DomainException` thrown by a handler as a failed `Result`.
Exceptions other than `DomainException` propagate as is.
See [Commands and Queries](../04-application/01-commands-and-queries.md) for details.

## Related APIs

- @D3Fragments.Result
- <xref:D3Fragments.Result`1>
- @D3Fragments.Error
- @D3Fragments.ErrorType
- @D3Fragments.DomainException
