using D3Fragments;
using D3Fragments.AspNetCore;

using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace BasicWebApp.Web;

public sealed class PageErrorHandler(ILogger<PageErrorHandler> logger, IOptions<DomainProblemDetailsOptions> options)
{
    public void AddToModelState(PageModel page, DomainException exception)
    {
        logger.LogInformation("Demo operation failed. ErrorType: {ErrorType}", exception.ErrorType);
        page.ModelState.AddDomainErrors(exception.Errors);
        page.Response.StatusCode = options.Value.GetStatusCode(exception.ErrorType);
    }
}