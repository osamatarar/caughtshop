using Caught.Data;
using Caught.Models;

namespace Caught.Services;

public sealed class SaleAlertService(Db db, ProductClickBuffer clicks)
{
    public async Task<SaleAlertResponse> Create(SaleAlertRequest request, CancellationToken cancellationToken)
    {
        if (request.ProductId <= 0)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Product was not found.");
        }

        var (channel, contact) = Rules.SaleContact(request.Channel, request.Contact);
        var row = await db.QuerySingle<SaleAlertRow>(
            "SELECT * FROM caught.insert_sale_alert(@ProductId, @Channel, @Contact)",
            new { request.ProductId, Channel = channel, Contact = contact },
            cancellationToken);
        if (row.Inserted)
        {
            clicks.Record(row.ProductId, "notification");
        }

        return new SaleAlertResponse
        {
            Id = row.Id,
            ProductId = row.ProductId,
            Channel = row.Channel,
            Contact = row.Contact,
        };
    }

    private sealed class SaleAlertRow
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Channel { get; set; } = "";
        public string Contact { get; set; } = "";
        public bool Inserted { get; set; }
    }
}
