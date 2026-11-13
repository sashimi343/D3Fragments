# Type-safe enums

`TypeSafeEnum<TSelf, TKey>` is an enumeration type whose values can each have their own data and behavior.
Unlike the built-in `enum`, no values exist other than the ones you define. You cannot create an invalid value by casting either.

## Defining an enumeration

Derive from `TypeSafeEnum<TSelf, TKey>` and define each value as a `static readonly` field.
`TKey` is the type of the key that identifies each value (such as the value stored in the database).
Make the constructor `private` so that values cannot be created outside the class.

```csharp
using D3Fragments;

public sealed class MemberType : TypeSafeEnum<MemberType, int>
{
    public static readonly MemberType Regular = new(0, "Regular", loanLimit: 5, loanPeriodDays: 14);
    public static readonly MemberType Student = new(1, "Student", loanLimit: 10, loanPeriodDays: 28);

    private MemberType(int key, string name, int loanLimit, int loanPeriodDays) : base(key, name)
    {
        LoanLimit = loanLimit;
        LoanPeriodDays = loanPeriodDays;
    }

    public int LoanLimit { get; }

    public int LoanPeriodDays { get; }
}
```

Keys and names must each be unique.
If they are duplicated, an exception is thrown when the type is initialized.

The analyzers warn about definition mistakes such as the following:

- The constructor is not `private` ([DFR0006](../../analyzers/DFR0006.md))
- A type other than the class itself is specified as `TSelf` ([DFR0007](../../analyzers/DFR0007.md))
- A value is defined somewhere other than a `static readonly` field ([DFR0008](../../analyzers/DFR0008.md))

To give each value different behavior, make `TSelf` an abstract class and define each value as a `private` nested class.

## Getting values

| Member | Description |
|---|---|
| `Key` / `Name` | The key and name (display name) of the value. If the name is omitted, the string representation of the key is used as the name |
| `Values` / `Keys` / `Count` | All defined values and keys, and their count (in definition order) |
| `Parse(key)` | Gets the value for a key. Throws an exception if it is not defined |
| `TryParse(key, out value)` | Tries to get the value for a key |
| `ParseAsResult(key)` | Returns the value for a key as `Result<T>`. If it is not defined, the result is a validation error |

To convert a key received from outside, use `TryParse` or `ParseAsResult`.

```csharp
if (!MemberType.TryParse(input.MemberTypeKey, out var memberType))
{
    ModelState.AddModelError(nameof(input.MemberTypeKey), "Select a member type.");
}
```

Each value has only one instance, so you can compare values with `==`.

## Persistence and JSON

- EF Core: value converters that persist the key are registered automatically for the type-safe enums in the assemblies passed to `AddD3FragmentsEFCore` (or `UseD3Fragments`).
- JSON: `TypeSafeEnumJsonConverterFactory` reads and writes values by key (default) or by name. Which one to use depends on the application, so `AddD3FragmentsAspNetCore` does not register it automatically. Register it as needed.

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new TypeSafeEnumJsonConverterFactory(TypeSafeEnumJsonFormat.Name)));
```

## Related APIs

- <xref:D3Fragments.TypeSafeEnum`2>
- @D3Fragments.TypeSafeEnumJsonConverterFactory
- @D3Fragments.TypeSafeEnumJsonFormat
