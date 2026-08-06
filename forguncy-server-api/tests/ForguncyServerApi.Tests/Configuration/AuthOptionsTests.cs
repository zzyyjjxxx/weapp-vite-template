using ForguncyServerApi.Configuration;
using Xunit;

namespace ForguncyServerApi.Tests.Configuration;

public sealed class AuthOptionsTests
{
    [Fact]
    public void AuthOptions_exposes_only_the_jwt_configuration_constructor()
    {
        var constructor = Assert.Single(typeof(AuthOptions).GetConstructors());

        Assert.Equal(
            new[]
            {
                typeof(string),
                typeof(string),
                typeof(TimeSpan)
            },
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Fact]
    public void From_requires_a_signing_key_but_not_a_connection_string()
    {
        var options = AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_JWT_SIGNING_KEY"] = new string('k', 32)
        });

        Assert.Equal(new string('k', 32), options.JwtSigningKey);
        Assert.Throws<ArgumentException>(() => AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_JWT_ISSUER"] = "synthetic-issuer"
        }));
    }

    [Fact]
    public void From_rejects_a_signing_key_shorter_than_32_characters()
    {
        var values = ValidValues();
        values["FGC_JWT_SIGNING_KEY"] = new string('k', 31);

        Assert.Throws<ArgumentException>(() => AuthOptions.From(values));
    }

    [Fact]
    public void From_uses_default_issuer_and_lifetime()
    {
        var options = AuthOptions.From(ValidValues());

        Assert.Equal("forguncy-server-api", options.JwtIssuer);
        Assert.Equal(TimeSpan.FromMinutes(60), options.JwtLifetime);
    }

    [Fact]
    public void From_parses_a_positive_expiration_in_minutes()
    {
        var values = ValidValues();
        values["FGC_JWT_EXPIRES_MINUTES"] = "15";

        Assert.Equal(TimeSpan.FromMinutes(15), AuthOptions.From(values).JwtLifetime);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    public void From_rejects_non_positive_and_non_numeric_expiration(string expiration)
    {
        var values = ValidValues();
        values["FGC_JWT_EXPIRES_MINUTES"] = expiration;

        Assert.Throws<ArgumentException>(() => AuthOptions.From(values));
    }

    [Fact]
    public void AuthOptions_source_does_not_read_environment_variables()
    {
        var source = File.ReadAllText(SourceFile("Configuration", "AuthOptions.cs"));

        Assert.DoesNotContain("FGC_AUTH_BOOTSTRAP_", source);
        Assert.DoesNotContain("Environment.GetEnvironmentVariable", source);
        Assert.DoesNotContain("FromEnvironment", source);
    }

    private static Dictionary<string, string?> ValidValues() => new()
    {
        ["FGC_JWT_SIGNING_KEY"] = new string('k', 32)
    };

    private static string SourceFile(params string[] segments) => Path.Combine(
        new[] { Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..")) }
            .Concat(segments)
            .ToArray());
}
