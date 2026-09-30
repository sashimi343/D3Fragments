using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace D3Fragments.AspNetCore;

// DomainException以外は処理せず、後続のハンドラ（最終的には標準の500応答）に任せる
internal sealed partial class DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException domainException)
        {
            return false;
        }

        LogDomainException(logger, domainException, domainException.ErrorType);
        await new DomainProblemResult(domainException.Errors, domainException.ErrorCode).ExecuteAsync(httpContext);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "A domain exception occurred. ErrorType: {ErrorType}")]
    private static partial void LogDomainException(ILogger logger, Exception exception, ErrorType errorType);
}
