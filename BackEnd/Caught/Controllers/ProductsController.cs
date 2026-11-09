using Caught.Models;
using Caught.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caught.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ProductService products) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> List(
        [FromQuery] int creatorId,
        CancellationToken cancellationToken)
    {
        return Ok(await products.List(creatorId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Get(int id, CancellationToken cancellationToken)
    {
        return Ok(await products.Get(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<ProductResponse>> Create(
        ProductWriteRequest request,
        CancellationToken cancellationToken)
    {
        var product = await products.Create(request, cancellationToken);
        return Created($"/api/products/{product.Id}", product);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ProductResponse>> Update(
        int id,
        ProductWriteRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await products.Update(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int creatorId, CancellationToken cancellationToken)
    {
        await products.Delete(id, creatorId, cancellationToken);
        return NoContent();
    }
}
