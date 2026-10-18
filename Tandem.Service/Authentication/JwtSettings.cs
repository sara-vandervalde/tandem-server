namespace Tandem.Service.Authentication;

// Bound from the "Jwt" configuration section. Issuer, Audience and lifetime live in appsettings.json;
// Key is a secret and comes only from User Secrets (or environment variables in production)
public class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;

    public string Audience { get; init; } = string.Empty;

    public string Key { get; init; } = string.Empty;

    public int ExpiresMinutes { get; init; }
}
