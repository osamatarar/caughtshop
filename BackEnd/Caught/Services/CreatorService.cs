using Caught.Data;
using Caught.Models;

namespace Caught.Services;

public sealed class CreatorService(Db db)
{
    public async Task<HandleAvailability> HandleAvailable(string? handle, CancellationToken cancellationToken)
    {
        var claimed = Rules.ClaimedHandle(handle);
        var row = await db.QuerySingle<AvailableRow>(
            "SELECT caught.handle_available(@Handle) AS available",
            new { Handle = claimed },
            cancellationToken);
        return new HandleAvailability
        {
            Handle = claimed,
            Available = row.Available,
        };
    }

    public async Task<CreatorProfile> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var fullName = Rules.Name(request.FullName, "Enter your name.", 120);
        var email = Rules.AccountEmail(request.Email);
        var password = Rules.Password(request.Password);
        var handle = Rules.ClaimedHandle(request.Handle);
        return await db.QuerySingle<CreatorProfile>(
            "SELECT * FROM caught.register_creator(@FullName, @Email, @PasswordHash, @Handle)",
            new
            {
                FullName = fullName,
                Email = email,
                PasswordHash = PasswordHasher.Hash(password),
                Handle = handle,
            },
            cancellationToken);
    }

    public async Task<CreatorProfile> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        var password = request.Password ?? "";
        if (email.Length == 0 || password.Length == 0)
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Email or password is incorrect.");
        }

        var row = await db.QueryOne<LoginRow>(
            "SELECT * FROM caught.creator_login_lookup(@Email)",
            new { Email = email },
            cancellationToken);
        if (row is null || !PasswordHasher.Verify(password, row.PasswordHash))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "Email or password is incorrect.");
        }

        return new CreatorProfile
        {
            CreatorId = row.CreatorId,
            FullName = row.FullName,
            Email = row.Email,
            Handle = row.Handle,
        };
    }

    public Task<CreatorSocialsResponse> UpdateProfile(
        int creatorId,
        CreatorSocialsRequest request,
        CancellationToken cancellationToken)
    {
        var id = Rules.CreatorId(creatorId);
        return db.QuerySingle<CreatorSocialsResponse>(
            """
            SELECT * FROM caught.update_creator_profile(
                @CreatorId, @TiktokUrl, @InstagramUrl, @YoutubeUrl)
            """,
            new
            {
                CreatorId = id,
                TiktokUrl = Rules.OptionalHttps(request.TiktokUrl),
                InstagramUrl = Rules.OptionalHttps(request.InstagramUrl),
                YoutubeUrl = Rules.OptionalHttps(request.YoutubeUrl),
            },
            cancellationToken);
    }

    private sealed class AvailableRow
    {
        public bool Available { get; set; }
    }

    private sealed class LoginRow
    {
        public int CreatorId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string Handle { get; set; } = "";
        public string PasswordHash { get; set; } = "";
    }
}
