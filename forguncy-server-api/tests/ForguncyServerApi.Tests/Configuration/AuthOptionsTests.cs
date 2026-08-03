using ForguncyServerApi.Configuration;
using Xunit;

namespace ForguncyServerApi.Tests.Configuration;

public sealed class AuthOptionsTests
{
    [Fact]
    public void From_requires_connection_string_and_signing_key()
    {
        Assert.Throws<ArgumentException>(() => AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_JWT_SIGNING_KEY"] = new string('k', 32)
        }));
        Assert.Throws<ArgumentException>(() => AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_AUTH_MYSQL_CONNECTION"] = "Server=synthetic;Database=test"
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
    public void From_accepts_bootstrap_values_only_as_an_optional_pair()
    {
        var values = ValidValues();
        values["FGC_BOOTSTRAP_USERNAME"] = "synthetic-admin";
        values["FGC_BOOTSTRAP_PASSWORD"] = "synthetic-password";

        var options = AuthOptions.From(values);

        Assert.Equal("synthetic-admin", options.BootstrapUsername);
        Assert.Equal("synthetic-password", options.BootstrapPassword);

        values.Remove("FGC_BOOTSTRAP_PASSWORD");
        Assert.Throws<ArgumentException>(() => AuthOptions.From(values));

        values = ValidValues();
        values["FGC_BOOTSTRAP_PASSWORD"] = "synthetic-password";
        Assert.Throws<ArgumentException>(() => AuthOptions.From(values));
    }

    private static Dictionary<string, string?> ValidValues() => new()
    {
        ["FGC_AUTH_MYSQL_CONNECTION"] = "Server=synthetic;Database=test",
        ["FGC_JWT_SIGNING_KEY"] = new string('k', 32)
    };
}
