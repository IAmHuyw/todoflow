using System.Net;

namespace Api.Authentication;

public sealed class GoogleOAuthOptions
{
    public const string SectionName = "GoogleOAuth";
    public const string ExternalCookieScheme = "GoogleExternal";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string FrontendUrl { get; set; } = "http://localhost:5173";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    public bool IsAllowedFrontendOrigin(string? value, bool allowLocalhost)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var candidate) ||
            candidate.Scheme is not ("http" or "https"))
        {
            return false;
        }

        var origin = candidate.GetLeftPart(UriPartial.Authority);
        if (string.Equals(origin, NormalizeOrigin(FrontendUrl), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return allowLocalhost &&
            (string.Equals(candidate.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
             IPAddress.TryParse(candidate.Host, out var address) && IPAddress.IsLoopback(address));
    }

    public string GetDefaultFrontendOrigin() => NormalizeOrigin(FrontendUrl);

    private static string NormalizeOrigin(string value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? uri.GetLeftPart(UriPartial.Authority)
            : "http://localhost:5173";
}
