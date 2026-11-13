# Input validation

D3Fragments provides validators that you use by declaring validation rules for a value.
The validation result is returned as a `Result` that contains the errors of every rule that was violated.

| Validator | Type to validate |
|---|---|
| `StringValidator` | `string` |
| `NumberValidator<T>` | Numeric types (`int`, `decimal`, and so on) |
| `DateOnlyValidator` | `DateOnly` |
| `DateTimeOffsetValidator` | `DateTimeOffset` |

## Declaring rules

Pass a display name (the field name used in error messages) to the constructor, and declare rules by chaining the rule methods.
Rules are meant to be declared once at the start and not changed afterward.
You can keep a validator in a `static readonly` field and reuse it.

```csharp
private static readonly StringValidator NameValidator =
    new StringValidator("Name", target: "name").Required().MaxLength(50);

private static readonly NumberValidator<int> QuantityValidator =
    new NumberValidator<int>("Quantity").Required().Range(1, 99);
```

The value specified for `target` is set as the `Target` of the errors.
Use it to associate errors with input fields (see [Razor Pages](../06-aspnetcore/02-razor-pages.md)).

## Validating

When you pass a value to `Check`, it is validated with all the declared rules and a `Result` is returned.

```csharp
Result result = NameValidator.Check(input.Name);
```

- A `null` value is treated as no value. (For `StringValidator` with `Required` declared, empty strings and whitespace-only strings are also treated as no value.)
- When there is no value, a validator with `Required` declared returns only the required error. Without `Required`, it skips all the rules and returns success.
- When there is a value, all the rules are evaluated and the errors of every violated rule are returned.

To localize the display name, for example, you can override it at validation time with `Check(value, displayName)`.

## Main rules

| Validator | Rules |
|---|---|
| `StringValidator` | `Required`, `NotNull`, `MinLength`, `MaxLength`, `Length`, `Matches`, `Email`, `OnlyCharacters`, `Must` |
| `NumberValidator<T>` | `Required`, `Min`, `Max`, `Range`, `Positive`, `NonNegative`, `Digits`, `Must` |
| `DateOnlyValidator` / `DateTimeOffsetValidator` | `Required`, `Min`, `Max`, `Range`, `Past`, `Future`, `Must` |

`Past` and `Future` get the current time from a `TimeProvider`.

For `OnlyCharacters`, specify the allowed kinds of characters with `CharacterKinds` flags (ASCII alphanumerics, hiragana, full-width katakana, and so on).

```csharp
new StringValidator("Member number").Required().Length(8).OnlyCharacters(CharacterKinds.AsciiDigit);
```

### How characters are counted

By default, `StringValidator` counts characters in UTF-16 code units (`string.Length`).
You can change how characters are counted with the `lengthUnit` constructor parameter.
Choose the one that matches how your database counts the length of the column.

| `StringLengthUnit` | Counts | Matching database type (example) |
|---|---|---|
| `Utf16CodeUnit` (default) | UTF-16 code units | SQL Server `nvarchar(n)` |
| `CodePoint` | Unicode code points | PostgreSQL and MySQL `varchar(n)` |
| `TextElement` | User-perceived characters (grapheme clusters) | — |

## Messages

You can change the error message with each rule's `message` parameter.
The message is a composite format string: `{0}` is replaced with the display name, and `{1}` and later with the rule's arguments.

```csharp
new StringValidator("Title").MaxLength(200, "{0} is too long (maximum {1} characters).");
```

If you omit `message`, the default message is used.
The default message is in Japanese or English, depending on `CultureInfo.CurrentUICulture` at validation time.

## Using validators in value objects

You can also use a validator in a value object's `ValidateValue`.

```csharp
private static readonly StringValidator Validator = new StringValidator("Title").Required().MaxLength(200);

public static bool ValidateValue(string value, [NotNullWhen(false)] out string? errorMessage)
{
    var result = Validator.Check(value);
    errorMessage = result.IsFailure ? result.GetFirstErrorMessage() : null;
    return result.IsSuccess;
}
```

## Related APIs

- @D3Fragments.Validations.StringValidator
- <xref:D3Fragments.Validations.NumberValidator`1>
- @D3Fragments.Validations.DateOnlyValidator
- @D3Fragments.Validations.DateTimeOffsetValidator
- @D3Fragments.Validations.CharacterKinds
- @D3Fragments.Validations.StringLengthUnit
