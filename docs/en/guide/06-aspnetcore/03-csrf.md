# CSRF protection

`UseCsrfProtection` is a middleware that rejects requests that do not have a specified custom request header.
It protects Web APIs that use cookie authentication against cross-site request forgery (CSRF).

## How it works

Before a browser adds a custom header to a request to another origin, it checks with a CORS preflight request whether the destination allows it.
If your CORS policy does not allow untrusted origins, pages on other sites cannot send requests with the header.
Therefore, by rejecting requests without the header, you avoid accepting requests sent from other sites.

The middleware checks only the following requests:

- The method is not GET, HEAD, OPTIONS, or TRACE
- The path is not under `BypassPaths`

For a request without the header, it returns 400 Bad Request as a Problem Details response.
The value of the header is not checked.

## Usage

```csharp
var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Apply only under /api
app.UseWhen(context => context.Request.Path.StartsWithSegments("/api"), api => api.UseCsrfProtection());
```

Clients add the header to requests with unsafe methods.

```javascript
await fetch("/api/holds/expire", {
  method: "POST",
  headers: { "X-Requested-By": "my-app" },
});
```

Razor Pages forms are protected by the standard ASP.NET Core antiforgery tokens.
Do not apply this middleware to form pages. If you do, form submissions, which cannot add the header, are rejected.

## Configuration

Configure it with `CsrfProtectionOptions`.

```csharp
builder.Services.Configure<CsrfProtectionOptions>(options =>
{
    options.RequiredHeaderName = "X-CSRF";
    options.BypassPaths.Add("/api/webhooks");
});
```

| Property | Description | Default |
|---|---|---|
| `RequiredHeaderName` | The name of the required header | `X-Requested-By` |
| `BypassPaths` | Path prefixes that are not checked (such as webhook endpoints). Matched by path segment | None |

See [OpenAPI](04-openapi.md) for how to include this header in OpenAPI documents.

## Related APIs

- @D3Fragments.AspNetCore.CsrfProtectionApplicationBuilderExtensions
- @D3Fragments.AspNetCore.CsrfProtectionOptions
