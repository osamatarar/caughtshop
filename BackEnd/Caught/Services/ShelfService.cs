using System.Text.Json;
using Caught.Data;
using Caught.Models;

namespace Caught.Services;

public sealed class ShelfService(Db db)
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<ShelfResponse> Get(string handle, CancellationToken cancellationToken)
    {
        var normalized = Rules.Handle(handle);
        var payload = await db.Scalar(
            "SELECT caught.shelf_by_handle(@Handle)::text",
            new { Handle = normalized },
            cancellationToken);
        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Creator was not found.");
        }

        return JsonSerializer.Deserialize<ShelfResponse>(payload, Json)
            ?? throw new ApiException(StatusCodes.Status404NotFound, "Creator was not found.");
    }

    public async Task<ProductResponse> Product(string handle, int number, CancellationToken cancellationToken)
    {
        var normalized = Rules.Handle(handle);
        if (number <= 0)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Product was not found.");
        }

        var product = await db.QueryOne<ProductResponse>(
            "SELECT * FROM caught.shelf_product(@Handle, @Number)",
            new { Handle = normalized, Number = number },
            cancellationToken);
        return product ?? throw new ApiException(StatusCodes.Status404NotFound, "Product was not found.");
    }
}
