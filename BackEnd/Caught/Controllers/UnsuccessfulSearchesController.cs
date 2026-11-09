using Caught.Models;
using Caught.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caught.Controllers;

[ApiController]
[Route("api/unsuccessful-searches")]
public sealed class UnsuccessfulSearchesController(UnsuccessfulSearchService searches) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(UnsuccessfulSearchRequest request, CancellationToken cancellationToken)
    {
        await searches.Record(request, cancellationToken);
        return NoContent();
    }

    [HttpGet]
    public async Task<ActionResult<UnsuccessfulSearchReportResponse>> Report(
        [FromQuery] int creatorId,
        [FromQuery] string? start,
        [FromQuery] string? end,
        [FromQuery] string? timeZone,
        CancellationToken cancellationToken)
    {
        return Ok(await searches.Report(creatorId, start, end, timeZone, cancellationToken));
    }
}
