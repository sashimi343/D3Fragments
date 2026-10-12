using System.Text.Json.Nodes;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace D3Fragments.AspNetCore.OpenApi;

internal static class CsrfProtectionHeaderTransformer
{
    // Swagger UI等で入力済みになるよう既定値を設定する。ミドルウェアはヘッダの値を検証しない
    private const string DefaultHeaderValue = "openapi";

    public static Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        var csrfOptions = context.ApplicationServices.GetRequiredService<IOptions<CsrfProtectionOptions>>().Value;
        var description = context.Description;
        if (description.HttpMethod is null || !csrfOptions.RequiresHeader(description.HttpMethod, "/" + description.RelativePath))
        {
            return Task.CompletedTask;
        }

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = csrfOptions.RequiredHeaderName,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Custom header for CSRF protection. Any value is accepted.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Default = JsonValue.Create(DefaultHeaderValue) },
        });
        return Task.CompletedTask;
    }
}
