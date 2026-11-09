using Caught.Data;
using Caught.Models;

namespace Caught.Services;

public sealed class VideoService(Db db)
{
    public Task<IReadOnlyList<VideoResponse>> List(int creatorId, CancellationToken cancellationToken)
    {
        var id = Rules.CreatorId(creatorId);
        return db.Query<VideoResponse>(
            "SELECT * FROM caught.list_videos(@CreatorId)",
            new { CreatorId = id },
            cancellationToken);
    }

    public async Task<VideoResponse> Get(int id, CancellationToken cancellationToken)
    {
        var videoId = Rules.PositiveId(id, "Video was not found.");
        var video = await db.QueryOne<VideoResponse>(
            "SELECT * FROM caught.get_video(@Id)",
            new { Id = videoId },
            cancellationToken);
        return video ?? throw new ApiException(StatusCodes.Status404NotFound, "Video was not found.");
    }

    public Task<VideoResponse> Create(VideoWriteRequest request, CancellationToken cancellationToken)
    {
        var draft = Clean(request);
        return db.QuerySingle<VideoResponse>(
            """
            SELECT * FROM caught.insert_video(
                @CreatorId, @TiktokUrl, @ThumbnailUrl, @Caption, @ProductId)
            """,
            draft,
            cancellationToken);
    }

    public Task<VideoResponse> Update(int id, VideoWriteRequest request, CancellationToken cancellationToken)
    {
        var videoId = Rules.PositiveId(id, "Video was not found.");
        var draft = Clean(request);
        return db.QuerySingle<VideoResponse>(
            """
            SELECT * FROM caught.update_video(
                @Id, @CreatorId, @TiktokUrl, @ThumbnailUrl, @Caption, @ProductId)
            """,
            new
            {
                Id = videoId,
                draft.CreatorId,
                draft.TiktokUrl,
                draft.ThumbnailUrl,
                draft.Caption,
                draft.ProductId,
            },
            cancellationToken);
    }

    public Task Delete(int id, int creatorId, CancellationToken cancellationToken)
    {
        var videoId = Rules.PositiveId(id, "Video was not found.");
        var ownerId = Rules.CreatorId(creatorId);
        return db.Execute(
            "SELECT caught.delete_video(@Id, @CreatorId)",
            new { Id = videoId, CreatorId = ownerId },
            cancellationToken);
    }

    private static VideoDraft Clean(VideoWriteRequest request)
    {
        if (request.ProductId <= 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose a product.");
        }

        return new VideoDraft(
            Rules.CreatorId(request.CreatorId),
            Rules.RequiredHttps(request.TiktokUrl, "Enter the TikTok video URL."),
            Rules.RequiredHttps(request.ThumbnailUrl, "Enter a thumbnail URL."),
            Rules.Name(request.Caption, "Enter a caption.", 2000),
            request.ProductId);
    }

    private sealed record VideoDraft(
        int CreatorId,
        string TiktokUrl,
        string ThumbnailUrl,
        string Caption,
        int ProductId);
}
