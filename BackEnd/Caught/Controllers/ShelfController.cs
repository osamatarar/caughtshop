using Caught.Models;
using Caught.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caught.Controllers;

[ApiController]
[Route("api/shelf")]
public sealed class ShelfController(ShelfService shelf) : ControllerBase
{
    [HttpGet("{handle}")]
    public async Task<ActionResult<ShelfResponse>> Get(string handle, CancellationToken cancellationToken)
    {
        return Ok(await shelf.Get(handle, cancellationToken));
    }

    [HttpGet("{handle}/products/{number:int}")]
    public async Task<ActionResult<ProductResponse>> Product(
        string handle,
        int number,
        CancellationToken cancellationToken)
    {
        return Ok(await shelf.Product(handle, number, cancellationToken));
    }
}
