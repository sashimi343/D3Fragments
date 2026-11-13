# Razor Pages

In Razor Pages and MVC views, errors are displayed together with the input form instead of being returned as Problem Details responses.
With `AddDomainErrors`, you can add domain errors to `ModelState`.

## Adding errors to ModelState

`ModelState.AddDomainErrors(errors)` adds each error to `ModelState`.

- An error with an `Error.Target` is added with the `Target` as its key. If the key matches the name of an input field, the error is displayed for that field.
- An error without a `Target` is added as a model-level error (with an empty key).

```csharp
public sealed class RegisterModel(IRequestExecutor<RegisterBookCommand, Book> registerBook) : PageModel
{
    [BindProperty]
    public BookInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Result<Book> result = await registerBook.TryExecuteAsync(Input.ToCommand(), HttpContext.RequestAborted);
        if (result.IsFailure)
        {
            ModelState.AddDomainErrors(result.Errors);
            return Page();
        }

        return RedirectToPage("Index");
    }
}
```

If you catch the `DomainException` instead of using `TryExecuteAsync`, pass `exception.Errors`.

```csharp
catch (DomainException exception)
{
    ModelState.AddDomainErrors(exception.Errors);
    Response.StatusCode = options.Value.GetStatusCode(exception.ErrorType);
    return Page();
}
```

In this example, the HTTP status code for the kind of error is obtained from `IOptions<DomainProblemDetailsOptions>` and set on the response.

## Associating errors with input fields

To display an error for a specific input field, specify `target` when you create the error.

```csharp
throw new DomainException(Error.Conflict("This email address is already registered.", target: "Input.EmailAddress"));
```

For validators, specify it with the `target` constructor parameter (see [Input validation](../03-domain/05-validation.md)).

## Related APIs

- @D3Fragments.AspNetCore.ModelStateDictionaryExtensions
- @D3Fragments.Application.RequestExecutorExtensions
