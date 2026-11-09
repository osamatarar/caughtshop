namespace Caught.Models;

public sealed class CreatorProfile
{
    public int CreatorId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Handle { get; set; } = "";
}

public sealed class RegisterRequest
{
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string Handle { get; set; } = "";
}

public sealed class HandleAvailability
{
    public string Handle { get; set; } = "";
    public bool Available { get; set; }
}

public sealed class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class CreatorSocialsRequest
{
    public string TiktokUrl { get; set; } = "";
    public string InstagramUrl { get; set; } = "";
    public string YoutubeUrl { get; set; } = "";
}

public sealed class CreatorSocialsResponse
{
    public int CreatorId { get; set; }
    public string TiktokUrl { get; set; } = "";
    public string InstagramUrl { get; set; } = "";
    public string YoutubeUrl { get; set; } = "";
}

public sealed class ProductResponse
{
    public int Id { get; set; }
    public int PublicNumber { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "";
    public string TiktokUrl { get; set; } = "";
    public string AmazonUrl { get; set; } = "";
    public string BrandUrl { get; set; } = "";
    public string Availability { get; set; } = "";
    public int? BackupProductId { get; set; }
}

public sealed class ProductWriteRequest
{
    public int CreatorId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public decimal Price { get; set; }
    public string Currency { get; set; } = "";
    public string TiktokUrl { get; set; } = "";
    public string AmazonUrl { get; set; } = "";
    public string BrandUrl { get; set; } = "";
    public string Availability { get; set; } = "";
    public int? BackupProductId { get; set; }
}

public sealed class VideoResponse
{
    public int Id { get; set; }
    public string TiktokUrl { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string Caption { get; set; } = "";
    public int ProductId { get; set; }
}

public sealed class VideoWriteRequest
{
    public int CreatorId { get; set; }
    public string TiktokUrl { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string Caption { get; set; } = "";
    public int ProductId { get; set; }
}

public sealed class SaleAlertResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string Channel { get; set; } = "";
    public string Contact { get; set; } = "";
}

public sealed class SaleAlertRequest
{
    public int ProductId { get; set; }
    public string Channel { get; set; } = "";
    public string Contact { get; set; } = "";
}

public sealed class ProductClickRequest
{
    public int ProductId { get; set; }
    public string Channel { get; set; } = "";
}

public sealed class ProductClickTotalsResponse
{
    public int Tiktok { get; set; }
    public int Amazon { get; set; }
    public int Brand { get; set; }
    public int Product { get; set; }
    public int Notification { get; set; }
}

public sealed class ProductClickDayResponse
{
    public string Date { get; set; } = "";
    public int Tiktok { get; set; }
    public int Amazon { get; set; }
    public int Brand { get; set; }
    public int Product { get; set; }
    public int Notification { get; set; }
}

public sealed class ProductClickProductResponse
{
    public int ProductId { get; set; }
    public int Tiktok { get; set; }
    public int Amazon { get; set; }
    public int Brand { get; set; }
    public int Product { get; set; }
    public int Notification { get; set; }
}

public sealed class ProductClickReportResponse
{
    public List<ProductClickDayResponse> Days { get; set; } = [];
    public List<ProductClickProductResponse> Products { get; set; } = [];
}

public sealed class UnsuccessfulSearchRequest
{
    public string Handle { get; set; } = "";
    public string Query { get; set; } = "";
}

public sealed class UnsuccessfulSearchDayResponse
{
    public string Date { get; set; } = "";
    public int Count { get; set; }
}

public sealed class UnsuccessfulSearchQueryResponse
{
    public string Query { get; set; } = "";
    public int Count { get; set; }
}

public sealed class UnsuccessfulSearchReportResponse
{
    public List<UnsuccessfulSearchDayResponse> Days { get; set; } = [];
    public List<UnsuccessfulSearchQueryResponse> Queries { get; set; } = [];
}

public sealed class ShelfResponse
{
    public int CreatorId { get; set; }
    public string FullName { get; set; } = "";
    public string Handle { get; set; } = "";
    public string TiktokUrl { get; set; } = "";
    public string InstagramUrl { get; set; } = "";
    public string YoutubeUrl { get; set; } = "";
    public List<ProductResponse> Products { get; set; } = [];
    public List<VideoResponse> Videos { get; set; } = [];
}
