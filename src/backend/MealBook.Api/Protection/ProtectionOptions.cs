using System.Net;
using System.Text.RegularExpressions;

namespace MealBook.Api.Protection;

// Tunable through the "Protection" configuration section (Protection__Api__PerMinute and so on).
public sealed class ProtectionOptions
{
    public const string Section = "Protection";

    public bool RateLimitingEnabled { get; init; } = true;
    public bool BlockAutomatedClients { get; init; } = true;

    // Header carrying the real visitor address when the app sits behind the website's reverse proxy.
    public string ClientIpHeader { get; init; } = "X-Forwarded-For";

    public LimitOptions Api { get; init; } = new() { PerMinute = 120, PerDay = 5000 };
    public LimitOptions Images { get; init; } = new() { PerMinute = 150, PerDay = 4000 };
    public LimitOptions Auth { get; init; } = new() { PerMinute = 10, PerDay = 100 };

    public sealed class LimitOptions
    {
        public int PerMinute { get; init; }
        public int PerDay { get; init; }
    }
}

public enum RequestClass
{
    None,
    Api,
    Images,
    Auth
}

public static partial class RequestClassifier
{
    public static RequestClass Classify(HttpRequest request)
    {
        var path = request.Path;
        if (!path.StartsWithSegments("/api")
            || path.StartsWithSegments("/api/health"))
        {
            return RequestClass.None;
        }

        if (path.StartsWithSegments("/api/identity")
            || path.StartsWithSegments("/api/auth/register")
            || path.StartsWithSegments("/api/recipe-imports"))
        {
            return RequestClass.Auth;
        }

        return HttpMethods.IsGet(request.Method) && RecipeImagePath().IsMatch(path.Value ?? string.Empty)
            ? RequestClass.Images
            : RequestClass.Api;
    }

    // Machine endpoints that legitimately receive non-browser clients.
    public static bool AllowsAutomation(HttpRequest request) =>
        request.Path.StartsWithSegments("/api/recipe-imports");

    [GeneratedRegex("^/api/recipes/[0-9a-fA-F-]{36}/image/?$")]
    private static partial Regex RecipeImagePath();
}

public static class ClientAddress
{
    private const string Unknown = "unknown";

    // The first valid address in the configured header, else the socket address. An unparsable
    // header value is ignored so it cannot be used to mint endless rate limit buckets.
    public static string Resolve(HttpContext context, string header)
    {
        if (!string.IsNullOrWhiteSpace(header)
            && context.Request.Headers.TryGetValue(header, out var values))
        {
            var first = values.ToString().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (first is not null && IPAddress.TryParse(first, out var forwarded))
            {
                return Normalize(forwarded);
            }
        }

        return context.Connection.RemoteIpAddress is { } remote ? Normalize(remote) : Unknown;
    }

    // IPv6 clients get a /64 bucket, because a single device can freely rotate inside its /64.
    private static string Normalize(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            return address.ToString();
        }

        var bytes = address.GetAddressBytes();
        Array.Clear(bytes, 8, 8);
        return new IPAddress(bytes) + "/64";
    }
}
