# Value objects

A value object is an immutable object that represents a value, such as a book title or an email address.
A value object in D3Fragments wraps a single value of a primitive type and validates it when it is created.

## Defining a value object

Derive from `ValueObjectBase<TSelf, TPrimitive>` and implement `IValueObject<TSelf, TPrimitive>`.
Specify the class itself as `TSelf` and the wrapped type as `TPrimitive`.

```csharp
using System.Diagnostics.CodeAnalysis;
using D3Fragments;

public sealed class EmailAddress : ValueObjectBase<EmailAddress, string>, IValueObject<EmailAddress, string>
{
    private EmailAddress(string value) : base(value)
    {
    }

    public static EmailAddress Create(string value) => new(value.Trim());

    public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errorMessage = "Specify an email address.";
            return false;
        }

        if (!System.Net.Mail.MailAddress.TryCreate(value, out _))
        {
            errorMessage = "The email address format is invalid.";
            return false;
        }

        errorMessage = null;
        return true;
    }
}
```

You implement the following two members.

| Member | Description |
|---|---|
| `static TSelf Create(TPrimitive value)` | Creates the value object from a primitive value. Normalize the value here (for example, trim leading and trailing whitespace) |
| `static bool ValidateValue(TPrimitive value, out string? errorMessage)` | Validates the value. If it is invalid, returns `false` and sets the reason in `errorMessage` |

The base class constructor calls `ValidateValue`.
Passing an invalid value throws `DomainValidationException`, so a value object in an invalid state cannot exist.

> [!NOTE]
> In C#, a base class cannot require derived classes to implement static methods.
> Therefore, you also declare `IValueObject`, which requires the implementation of `Create` and `ValidateValue`.
> If you forget to declare `IValueObject`, you get a compile error.

## Creating a value object

There are two ways to create a value object.

```csharp
// Throws DomainValidationException if the value is invalid
var email = EmailAddress.Create("user@example.com");

// Returns a failed Result if the value is invalid
Result<EmailAddress> result = EmailAddress.TryCreate(input);
if (result.IsFailure)
{
    // result.Errors contains the validation errors
}
```

`TryCreate` is provided by the base class.
Use it when you want to validate several values together and return all of the errors.

```csharp
public static Result<Book> TryCreate(string isbn, string title, string author)
{
    Result<Isbn> isbnResult = Isbn.TryCreate(isbn);
    Result<BookTitle> titleResult = BookTitle.TryCreate(title);
    if (isbnResult.IsFailure || titleResult.IsFailure)
    {
        return Result<Book>.Failure(Result.CombineErrors(isbnResult, titleResult));
    }

    return Result<Book>.Success(new Book(isbnResult.Value, titleResult.Value, author, BookStatus.Available));
}
```

## Equality and string representation

Value objects that wrap equal values are considered equal (`Equals` and `==`).
`ToString()` returns the string representation of the wrapped value.

## Persistence and JSON

Value objects can be converted to external representations as follows.

- EF Core: `ComplexValueObject` maps a value object to a single column named after the property. See [EF Core integration](../05-efcore/01-dbcontext.md) for details.
- JSON: `ValueObjectJsonConverterFactory` reads and writes a value object as its wrapped value (such as `"user@example.com"`). Calling `AddD3FragmentsAspNetCore` registers it with ASP.NET Core automatically. Values read from JSON are also validated with `ValidateValue`.

## Related APIs

- <xref:D3Fragments.ValueObjectBase`2>
- <xref:D3Fragments.IValueObject`2>
- @D3Fragments.ValueObjectJsonConverterFactory
- @D3Fragments.DomainValidationException
