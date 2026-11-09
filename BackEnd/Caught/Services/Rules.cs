using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Caught.Services;

internal static partial class Rules
{
    private static readonly HashSet<string> Currencies = new(StringComparer.Ordinal)
    {
        "USD", "EUR", "GBP", "CAD", "AUD",
    };

    private static readonly HashSet<string> ReservedHandles = new(StringComparer.Ordinal)
    {
        "login", "signup", "studio", "dashboard", "api", "assets", "health", "admin",
    };

    private const decimal MaxPrice = 9999999999.99m;

    public static string Name(string? value, string emptyMessage, int maxLength)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, emptyMessage);
        }

        if (text.Length > maxLength)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Use a shorter value.");
        }

        return text;
    }

    public static string AccountEmail(string? value)
    {
        var email = NormalizeEmail(value);
        if (email is null)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Enter a valid email.");
        }

        return email;
    }

    public static string Password(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Enter your password.");
        }

        if (value.Length > 200)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Use a shorter password.");
        }

        return value;
    }

    public static int CreatorId(int creatorId)
    {
        if (creatorId <= 0)
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Creator was not found.");
        }

        return creatorId;
    }

    public static int PositiveId(int id, string message)
    {
        if (id <= 0)
        {
            throw new ApiException(StatusCodes.Status404NotFound, message);
        }

        return id;
    }

    public static string RequiredHttps(string? value, string emptyMessage)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, emptyMessage);
        }

        if (!IsHttps(text))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Use an https link.");
        }

        return text;
    }

    public static string OptionalHttps(string? value)
    {
        var text = value?.Trim() ?? "";
        if (text.Length == 0)
        {
            return "";
        }

        if (!IsHttps(text))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Use an https link.");
        }

        return text;
    }

    public static decimal Price(decimal price)
    {
        if (price <= 0 || price > MaxPrice || decimal.Round(price, 2) != price)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Use a price above zero, with at most two decimals.");
        }

        return price;
    }

    public static string Currency(string? value)
    {
        var code = value?.Trim().ToUpperInvariant() ?? "";
        if (!Currencies.Contains(code))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose a currency.");
        }

        return code;
    }

    public static string Availability(string? value)
    {
        var availability = value?.Trim().ToLowerInvariant() ?? "";
        if (availability is not ("available" or "out_of_stock"))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "Choose availability.");
        }

        return availability;
    }

    public static string Handle(string? value)
    {
        var handle = value?.Trim().ToLowerInvariant() ?? "";
        if (!HandlePattern().IsMatch(handle))
        {
            throw new ApiException(StatusCodes.Status404NotFound, "Creator was not found.");
        }

        return handle;
    }

    public static string ClaimedHandle(string? value)
    {
        var handle = value?.Trim().ToLowerInvariant() ?? "";
        if (handle.StartsWith('@'))
        {
            handle = handle[1..];
        }

        if (!HandlePattern().IsMatch(handle))
        {
            throw new ApiException(
                StatusCodes.Status400BadRequest,
                "Use 3-30 letters, numbers, or underscores.");
        }

        if (ReservedHandles.Contains(handle))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "That link can't be used.");
        }

        return handle;
    }

    public static (string Channel, string Contact) SaleContact(string? channel, string? contact)
    {
        if (channel == "email")
        {
            var email = NormalizeEmail(contact);
            if (email is null)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Enter a valid email.");
            }

            return ("email", email);
        }

        if (channel == "phone")
        {
            var phone = NormalizePhone(contact);
            if (phone is null)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "Enter a valid phone number.");
            }

            return ("phone", phone);
        }

        throw new ApiException(StatusCodes.Status400BadRequest, "Enter an email or a phone number.");
    }

    private static string? NormalizeEmail(string? value)
    {
        var email = value?.Trim().ToLowerInvariant() ?? "";
        if (!EmailPattern().IsMatch(email))
        {
            return null;
        }

        try
        {
            var address = new MailAddress(email);
            return address.Address == email ? email : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static string? NormalizePhone(string? value)
    {
        var trimmed = value?.Trim() ?? "";
        var match = PhonePattern().Match(trimmed);
        if (!match.Success)
        {
            return null;
        }

        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        if (digits.Length is < 10 or > 15)
        {
            return null;
        }

        return match.Groups[1].Success ? $"+{digits}" : digits;
    }

    private static bool IsHttps(string value)
    {
        return value.Length <= 2000
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Length > 0;
    }

    [GeneratedRegex(
        @"^(?=.{1,254}$)(?=.{1,64}@)[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-zA-Z0-9!#$%&'*+/=?^_`{|}~-]+)*@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$",
        RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^(\+)?[\d\s().-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex PhonePattern();

    [GeneratedRegex("^[a-z0-9_]{3,30}$", RegexOptions.CultureInvariant)]
    private static partial Regex HandlePattern();
}
