using BasicWebApp.Application.Books;

using D3Fragments.Application;

using Microsoft.AspNetCore.Mvc;

namespace BasicWebApp.Web.Controllers;

[ApiController]
[Route("api/holds")]
public sealed class HoldsController(IRequestExecutor<ExpireHoldsCommand, int> expireHolds) : ControllerBase
{
    [HttpPost("expire")]
    public async Task<IActionResult> ExpireAsync(CancellationToken cancellationToken)
    {
        int processedCount = await expireHolds.ExecuteAsync(new ExpireHoldsCommand(), cancellationToken);
        return Ok(new { processedCount });
    }
}