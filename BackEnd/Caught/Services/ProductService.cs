using Caught.Data;
using Caught.Models;

namespace Caught.Services;

public sealed class ProductService(Db db)
{
    public Task<IReadOnlyList<ProductResponse>> List(int creatorId, CancellationToken cancellationToken)
    {
        var id = Rules.CreatorId(creatorId);
        return db.Query<ProductResponse>(
            "SELECT * FROM caught.list_products(@CreatorId)",
            new { CreatorId = id },
            cancellationToken);
    }

    public async Task<ProductResponse> Get(int id, CancellationToken cancellationToken)
    {
        var productId = Rules.PositiveId(id, "Product was not found.");
        var product = await db.QueryOne<ProductResponse>(
            "SELECT * FROM caught.get_product(@Id)",
            new { Id = productId },
            cancellationToken);
        return product ?? throw new ApiException(StatusCodes.Status404NotFound, "Product was not found.");
    }

    public Task<ProductResponse> Create(ProductWriteRequest request, CancellationToken cancellationToken)
    {
        var draft = Clean(request);
        return db.QuerySingle<ProductResponse>(
            """
            SELECT * FROM caught.insert_product(
                @CreatorId, @Name, @Description, @ImageUrl, @Price, @Currency,
                @TiktokUrl, @AmazonUrl, @BrandUrl, @Availability, @BackupProductId)
            """,
            draft,
            cancellationToken);
    }

    public Task<ProductResponse> Update(int id, ProductWriteRequest request, CancellationToken cancellationToken)
    {
        var productId = Rules.PositiveId(id, "Product was not found.");
        var draft = Clean(request);
        return db.QuerySingle<ProductResponse>(
            """
            SELECT * FROM caught.update_product(
                @Id, @CreatorId, @Name, @Description, @ImageUrl, @Price, @Currency,
                @TiktokUrl, @AmazonUrl, @BrandUrl, @Availability, @BackupProductId)
            """,
            new
            {
                Id = productId,
                draft.CreatorId,
                draft.Name,
                draft.Description,
                draft.ImageUrl,
                draft.Price,
                draft.Currency,
                draft.TiktokUrl,
                draft.AmazonUrl,
                draft.BrandUrl,
                draft.Availability,
                draft.BackupProductId,
            },
            cancellationToken);
    }

    public Task Delete(int id, int creatorId, CancellationToken cancellationToken)
    {
        var productId = Rules.PositiveId(id, "Product was not found.");
        var ownerId = Rules.CreatorId(creatorId);
        return db.Execute(
            "SELECT caught.delete_product(@Id, @CreatorId)",
            new { Id = productId, CreatorId = ownerId },
            cancellationToken);
    }

    private static ProductDraft Clean(ProductWriteRequest request)
    {
        var backup = request.BackupProductId;
        if (backup is <= 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose a different backup product.");
        }

        return new ProductDraft(
            Rules.CreatorId(request.CreatorId),
            Rules.Name(request.Name, "Enter a name.", 200),
            Rules.Name(request.Description, "Enter a description.", 2000),
            Rules.RequiredHttps(request.ImageUrl, "Enter an image URL."),
            Rules.Price(request.Price),
            Rules.Currency(request.Currency),
            Rules.RequiredHttps(request.TiktokUrl, "Enter the TikTok Shop URL."),
            Rules.OptionalHttps(request.AmazonUrl),
            Rules.OptionalHttps(request.BrandUrl),
            Rules.Availability(request.Availability),
            backup);
    }

    private sealed record ProductDraft(
        int CreatorId,
        string Name,
        string Description,
        string ImageUrl,
        decimal Price,
        string Currency,
        string TiktokUrl,
        string AmazonUrl,
        string BrandUrl,
        string Availability,
        int? BackupProductId);
}
