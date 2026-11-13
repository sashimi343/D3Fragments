using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace D3Fragments.AspNetCore;

// DomainExceptionとResultの失敗の両方から、同じ形式のProblem Detailsを書き出す
internal sealed class DomainProblemResult(IReadOnlyList<Error> errors, string? errorCode) : IResult
{
    public Task ExecuteAsync(HttpContext httpContext)
    {
        var options = httpContext.RequestServices.GetRequiredService<IOptions<DomainProblemDetailsOptions>>().Value;
        var errorType = errors[0].Type;
        var problem = new ProblemDetails
        {
            Status = options.GetStatusCode(errorType),
            Title = errorType.ToString(),
            Detail = string.Join('\n', errors.Select(e => e.Message)),
        };
        if (errorCode is not null)
        {
            problem.Extensions[options.ErrorCodeKey] = errorCode;
        }

        // IProblemDetailsServiceが登録されていればそれを通し（traceId付与やCustomizeProblemDetailsが効く）、
        // 書き出せない場合（Acceptヘッダの不一致等）でもJSONで応答する
        return TypedResults.Problem(problem).ExecuteAsync(httpContext);
    }
}
