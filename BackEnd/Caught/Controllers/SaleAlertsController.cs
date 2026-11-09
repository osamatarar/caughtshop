using Caught.Models;
using Caught.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caught.Controllers;

[ApiController]
[Route("api/sale-alerts")]
public sealed class SaleAlertsController(SaleAlertService alerts) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SaleAlertResponse>> Create(
        SaleAlertRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await alerts.Create(request, cancellationToken));
    }
}
