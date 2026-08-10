using System.Globalization;

namespace ForguncyServerApi.Configuration;

public sealed record AuthOptions(
    string JwtSigningKey,
    string JwtIssuer,
    TimeSpan JwtLifetime,
    TimeSpan JwtRefreshLifetime)
{
    public static AuthOptions From(IReadOnlyDictionary<string, string?> values)
    {
        if (values is null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var signingKey = Required(values, "FGC_JWT_SIGNING_KEY");
        if (signingKey.Length < 32)
        {
            throw new ArgumentException("FGC_JWT_SIGNING_KEY must be at least 32 characters.", nameof(values));
        }

        var issuer = Optional(values, "FGC_JWT_ISSUER") ?? "forguncy-server-api";
        var lifetime = ParseLifetime(values);
        var refreshLifetime = ParseRefreshLifetime(values);

        return new AuthOptions(signingKey, issuer, lifetime, refreshLifetime);
    }

    private static string Required(IReadOnlyDictionary<string, string?> values, string name)
    {
        var value = Optional(values, name);
        return value ?? throw new ArgumentException($"{name} is required.", nameof(values));
    }

    private static string? Optional(IReadOnlyDictionary<string, string?> values, string name)
    {
        return values.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : null;
    }

    private static TimeSpan ParseLifetime(IReadOnlyDictionary<string, string?> values)
    {
        var raw = Optional(values, "FGC_JWT_EXPIRES_MINUTES");
        if (raw is null)
        {
            return TimeSpan.FromMinutes(60);
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) || minutes <= 0)
        {
            throw new ArgumentException("FGC_JWT_EXPIRES_MINUTES must be a positive integer.", nameof(values));
        }

        try
        {
            return TimeSpan.FromMinutes(minutes);
        }
        catch (OverflowException exception)
        {
            throw new ArgumentException("FGC_JWT_EXPIRES_MINUTES is out of range.", nameof(values), exception);
        }
    }

    private static TimeSpan ParseRefreshLifetime(IReadOnlyDictionary<string, string?> values)
    {
        var raw = Optional(values, "FGC_JWT_REFRESH_EXPIRES_MINUTES");
        if (raw is null)
        {
            return TimeSpan.FromMinutes(10080);
        }

        if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes) || minutes <= 0)
        {
            throw new ArgumentException("FGC_JWT_REFRESH_EXPIRES_MINUTES must be a positive integer.", nameof(values));
        }

        try
        {
            return TimeSpan.FromMinutes(minutes);
        }
        catch (OverflowException exception)
        {
            throw new ArgumentException("FGC_JWT_REFRESH_EXPIRES_MINUTES is out of range.", nameof(values), exception);
        }
    }
}
