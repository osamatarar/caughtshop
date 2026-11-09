using Caught.Models;
using Caught.Services;
using Microsoft.AspNetCore.Mvc;

namespace Caught.Controllers;

[ApiController]
[Route("api/videos")]
public sealed class VideosController(VideoService videos) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VideoResponse>>> List(
        [FromQuery] int creatorId,
        CancellationToken cancellationToken)
    {
        return Ok(await videos.List(creatorId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<VideoResponse>> Get(int id, CancellationToken cancellationToken)
    {
        return Ok(await videos.Get(id, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<VideoResponse>> Create(
        VideoWriteRequest request,
        CancellationToken cancellationToken)
    {
        var video = await videos.Create(request, cancellationToken);
        return Created($"/api/videos/{video.Id}", video);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<VideoResponse>> Update(
        int id,
        VideoWriteRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await videos.Update(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int creatorId, CancellationToken cancellationToken)
    {
        await videos.Delete(id, creatorId, cancellationToken);
        return NoContent();
    }
}
