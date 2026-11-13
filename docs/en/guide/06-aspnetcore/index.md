# ASP.NET Core integration

The `Sashimi343.D3Fragments.AspNetCore` package provides features for using D3Fragments in the presentation layer.
If you generate OpenAPI documents, also use the `Sashimi343.D3Fragments.AspNetCore.OpenApi` package.

| Page | Contents |
|---|---|
| [Error responses](01-error-responses.md) | Converts `DomainException` and failed `Result`s into Problem Details responses |
| [Razor Pages](02-razor-pages.md) | Reflects domain errors in `ModelState` |
| [CSRF protection](03-csrf.md) | Protects cookie-authenticated APIs by requiring a custom header |
| [OpenAPI](04-openapi.md) | Aligns the schemas in OpenAPI documents with the D3Fragments types |

## Service registration

```csharp
builder.Services.AddD3FragmentsAspNetCore();

var app = builder.Build();

app.UseExceptionHandler();
```

`AddD3FragmentsAspNetCore` registers an exception handler that converts `DomainException` into Problem Details responses, the JSON converter for value objects, and so on.
To enable the exception handler, call `app.UseExceptionHandler()`.
Exceptions other than `DomainException` are left to the standard ASP.NET Core exception handling.
See [Service registration](../02-service-registration.md) for the list of registered services.
