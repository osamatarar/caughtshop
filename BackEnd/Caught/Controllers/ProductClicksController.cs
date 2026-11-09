using Caught.Models;
using Caught.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caught.Controllers;

[ApiController]
[Route("api/product-clicks")]
public sealed class ProductClicksController(ProductClickService clicks) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(ProductClickRequest request, CancellationToken cancellationToken)
    {
        await clicks.Record(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("totals")]
    public async Task<ActionResult<ProductClickTotalsResponse>> Totals(
        [FromQuery] int creatorId,
        [FromQuery] int productId,
        CancellationToken cancellationToken)
    {
        return Ok(await clicks.Totals(creatorId, productId, cancellationToken));
    }

    [HttpGet]
    public async Task<ActionResult<ProductClickReportResponse>> Report(
        [FromQuery] int creatorId,
        [FromQuery] string? start,
        [FromQuery] string? end,
        [FromQuery] string? timeZone,
        CancellationToken cancellationToken)
    {
        return Ok(await clicks.Report(creatorId, start, end, timeZone, cancellationToken));
    }
}
