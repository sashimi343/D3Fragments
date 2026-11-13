using System.Security.Claims;

using D3Fragments;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BasicWebApp.Web.Pages;

public abstract class AppPageModel(PageErrorHandler errors) : PageModel
{
    protected UuidV7 MemberId => UuidV7.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, null);

    protected string MemberEmail => User.FindFirstValue(ClaimTypes.Email)!;

    protected async Task<IActionResult> ExecuteWithErrorsAsync(Func<Task<IActionResult>> action, Func<Task>? reload = null)
    {
        try
        {
            return await action();
        }
        catch (DomainException exception)
        {
            errors.AddToModelState(this, exception);
            if (reload is not null)
            {
                try
                {
                    await reload();
                }
                catch (DomainException reloadException)
                {
                    errors.AddToModelState(this, reloadException);
                }
            }
            return Page();
        }
    }

    protected PageResult InvalidInput()
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return Page();
    }
}