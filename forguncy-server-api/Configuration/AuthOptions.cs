using System.Globalization;

namespace ForguncyServerApi.Configuration;

public sealed record AuthOptions(
    string JwtSigningKey,
    string JwtIssuer,
    TimeSpan JwtLifetime,
    string? BootstrapUsername,
    string? BootstrapPassword)
{
    public AuthOptions(
        string ignoredConnectionString,
        string jwtSigningKey,
        string jwtIssuer,
        TimeSpan jwtLifetime,
        string? bootstrapUsername,
        string? bootstrapPassword)
        : this(jwtSigningKey, jwtIssuer, jwtLifetime, bootstrapUsername, bootstrapPassword)
    {
    }

    public static AuthOptions From(IReadOnlyDictionary<string, string?> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var signingKey = Required(values, "FGC_JWT_SIGNING_KEY");
        if (signingKey.Length < 32)
        {
            throw new ArgumentException("FGC_JWT_SIGNING_KEY must be at least 32 characters.", nameof(values));
        }

        var issuer = Optional(values, "FGC_JWT_ISSUER") ?? "forguncy-server-api";
        var lifetime = ParseLifetime(values);
        var bootstrapUsername = Optional(values, "FGC_AUTH_BOOTSTRAP_USERNAME");
        var bootstrapPassword = Optional(values, "FGC_AUTH_BOOTSTRAP_PASSWORD");
        if ((bootstrapUsername is null) != (bootstrapPassword is null))
        {
            throw new ArgumentException(
                "FGC_AUTH_BOOTSTRAP_USERNAME and FGC_AUTH_BOOTSTRAP_PASSWORD must be provided together.",
                nameof(values));
        }

        return new AuthOptions(
            signingKey,
            issuer,
            lifetime,
            bootstrapUsername,
            bootstrapPassword);
    }

    public static AuthOptions FromEnvironment()
    {
        var names = new[]
        {
            "FGC_JWT_SIGNING_KEY",
            "FGC_JWT_ISSUER",
            "FGC_JWT_EXPIRES_MINUTES",
            "FGC_AUTH_BOOTSTRAP_USERNAME",
            "FGC_AUTH_BOOTSTRAP_PASSWORD"
        };

        return From(names.ToDictionary(name => name, Environment.GetEnvironmentVariable));
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
}
