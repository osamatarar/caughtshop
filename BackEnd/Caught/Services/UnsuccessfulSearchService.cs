using System.Globalization;
using Caught.Data;
using Caught.Models;

namespace Caught.Services;

public sealed class UnsuccessfulSearchService(Db db)
{
    public async Task Record(UnsuccessfulSearchRequest request, CancellationToken cancellationToken)
    {
        var handle = Rules.Handle(request.Handle);
        var query = SearchQuery(request.Query);
        await db.Execute(
            "SELECT caught.insert_unsuccessful_search(@Handle, @Query)",
            new { Handle = handle, Query = query },
            cancellationToken);
    }

    public async Task<UnsuccessfulSearchReportResponse> Report(
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
            "SELECT * FROM caught.unsuccessful_search_report(@CreatorId, @Start, @End, @TimeZone)",
            new { CreatorId = ownerId, Start = windowStart, End = windowEnd, TimeZone = zone },
            cancellationToken);

        var report = new UnsuccessfulSearchReportResponse();
        foreach (var row in rows)
        {
            if (row.Kind == "day")
            {
                report.Days.Add(new UnsuccessfulSearchDayResponse
                {
                    Date = row.DayKey ?? "",
                    Count = row.SearchCount,
                });
                continue;
            }

            if (row.Kind == "query" && row.QueryText is not null)
            {
                report.Queries.Add(new UnsuccessfulSearchQueryResponse
                {
                    Query = row.QueryText,
                    Count = row.SearchCount,
                });
            }
        }

        return report;
    }

    private static string SearchQuery(string? value)
    {
        var stripped = (value ?? "").Trim().TrimStart('#').Trim();
        if (stripped.Length == 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Enter a search term.");
        }

        var isNumber = stripped.Length <= 6 && stripped.All(char.IsDigit);
        if (!isNumber && stripped.Length < 2)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Enter a search term.");
        }

        if (stripped.Length > 200)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Use a shorter search term.");
        }

        return isNumber ? stripped : stripped.ToLowerInvariant();
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
        public string? DayKey { get; set; }
        public string? QueryText { get; set; }
        public int SearchCount { get; set; }
    }
}
