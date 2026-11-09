using System.Globalization;
using Caught.Data;
using Caught.Models;

namespace Caught.Services;

public sealed class ProductClickService(Db db, ProductClickBuffer buffer)
{
    public Task Record(ProductClickRequest request, CancellationToken cancellationToken)
    {
        var productId = Rules.PositiveId(request.ProductId, "Product was not found.");
        var channel = Channel(request.Channel);
        buffer.Record(productId, channel);
        return Task.CompletedTask;
    }

    public async Task<ProductClickTotalsResponse> Totals(
        int creatorId,
        int productId,
        CancellationToken cancellationToken)
    {
        var ownerId = Rules.CreatorId(creatorId);
        var id = Rules.PositiveId(productId, "Product was not found.");
        var totals = await db.QueryOne<ProductClickTotalsResponse>(
            "SELECT * FROM caught.product_click_totals(@CreatorId, @ProductId)",
            new { CreatorId = ownerId, ProductId = id },
            cancellationToken);
        return totals ?? throw new ApiException(StatusCodes.Status404NotFound, "Product was not found.");
    }

    public async Task<ProductClickReportResponse> Report(
        int creatorId,
        string? start,
        string? end,
        string? timeZone,
        CancellationToken cancellationToken)
    {
        var ownerId = Rules.CreatorId(creatorId);
        var windowStart = Date(start);
        var windowEnd = Date(end);
        if (windowStart is null || windowEnd is null || windowEnd < windowStart)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Start date must be on or before the end date.");
        }

        if (windowEnd.Value.DayNumber - windowStart.Value.DayNumber > 365)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose a range of 366 days or fewer.");
        }

        var zone = TimeZone(timeZone);
        var rows = await db.Query<ReportRow>(
            "SELECT * FROM caught.product_click_report(@CreatorId, @Start, @End, @TimeZone)",
            new { CreatorId = ownerId, Start = windowStart, End = windowEnd, TimeZone = zone },
            cancellationToken);

        var report = new ProductClickReportResponse();
        foreach (var row in rows)
        {
            if (row.Kind == "day")
            {
                report.Days.Add(new ProductClickDayResponse
                {
                    Date = row.ClickedOn ?? "",
                    Tiktok = row.Tiktok,
                    Amazon = row.Amazon,
                    Brand = row.Brand,
                    Product = row.Product,
                    Notification = row.Notification,
                });
                continue;
            }

            if (row.Kind == "product" && row.ProductId is int id)
            {
                report.Products.Add(new ProductClickProductResponse
                {
                    ProductId = id,
                    Tiktok = row.Tiktok,
                    Amazon = row.Amazon,
                    Brand = row.Brand,
                    Product = row.Product,
                    Notification = row.Notification,
                });
            }
        }

        return report;
    }

    private static string Channel(string? value)
    {
        var channel = value?.Trim().ToLowerInvariant() ?? "";
        if (channel is not ("tiktok" or "amazon" or "brand" or "product"))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose a store.");
        }

        return channel;
    }

    private static DateOnly? Date(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateOnly.TryParseExact(
            value.Trim(),
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    private static string TimeZone(string? value)
    {
        var zone = value?.Trim() ?? "";
        if (!IsTimeZoneName(zone))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose a time zone.");
        }

        return zone;
    }

    private static bool IsTimeZoneName(string zone)
    {
        if (zone.Length is 0 or > 64)
        {
            return false;
        }

        foreach (var character in zone)
        {
            if (character is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '/' or '+' or '-')
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private sealed class ReportRow
    {
        public string Kind { get; set; } = "";
        public string? ClickedOn { get; set; }
        public int? ProductId { get; set; }
        public int Tiktok { get; set; }
        public int Amazon { get; set; }
        public int Brand { get; set; }
        public int Product { get; set; }
        public int Notification { get; set; }
    }
}
