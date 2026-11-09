using Caught.Models;
using Caught.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caught.Controllers;

[ApiController]
[Route("api/creators")]
public sealed class CreatorsController(CreatorService creators) : ControllerBase
{
    [HttpGet("handles/{handle}")]
    public async Task<ActionResult<HandleAvailability>> HandleAvailable(
        string handle,
        CancellationToken cancellationToken)
    {
        return Ok(await creators.HandleAvailable(handle, cancellationToken));
    }

    [HttpPost("register")]
    public async Task<ActionResult<CreatorProfile>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var profile = await creators.Register(request, cancellationToken);
        return Created($"/api/shelf/{profile.Handle}", profile);
    }

    [HttpPost("login")]
    public async Task<ActionResult<CreatorProfile>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        return Ok(await creators.Login(request, cancellationToken));
    }

    [HttpPut("{creatorId:int}/profile")]
    public async Task<ActionResult<CreatorSocialsResponse>> UpdateProfile(
        int creatorId,
        CreatorSocialsRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await creators.UpdateProfile(creatorId, request, cancellationToken));
    }
}
