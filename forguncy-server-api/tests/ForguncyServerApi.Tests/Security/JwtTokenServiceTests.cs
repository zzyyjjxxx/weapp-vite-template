using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Security;
using Xunit;

namespace ForguncyServerApi.Tests.Security;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_contains_user_claims_and_validate_returns_them()
    {
        var user = new AuthUser { Id = 7, Username = "demo", IsOpen = 1 };
        var service = new JwtTokenService(TestOptions());

        var token = service.CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var principal = service.ValidateToken(token);

        Assert.Equal("access", Assert.Single(jwt.Claims.Where(claim => claim.Type == "token_use")).Value);
        Assert.Equal("7", principal.FindFirst("sub")?.Value);
        Assert.Equal("demo", principal.FindFirst("name")?.Value);
    }

    [Fact]
    public void CreateRefreshToken_contains_refresh_claims_and_validate_refresh_returns_them()
    {
        var user = new AuthUser { Id = 7, Username = "demo", IsOpen = 1 };
        var service = new JwtTokenService(TestOptions());

        var token = service.CreateRefreshToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var principal = service.ValidateRefreshToken(token);

        Assert.Equal("refresh", Assert.Single(jwt.Claims.Where(claim => claim.Type == "token_use")).Value);
        Assert.Equal("7", principal.FindFirst("sub")?.Value);
        Assert.Equal("demo", principal.FindFirst("name")?.Value);
    }

    [Fact]
    public void ValidateRefreshToken_rejects_an_access_token()
    {
        var service = new JwtTokenService(TestOptions());
        var token = service.CreateToken(new AuthUser { Id = 1, Username = "demo" });

        AssertIdentityModelException(
            () => service.ValidateRefreshToken(token),
            "SecurityTokenException");
    }

    [Fact]
    public void CreateToken_and_CreateRefreshToken_use_the_configured_lifetimes()
    {
        var service = new JwtTokenService(TestOptions(accessLifetimeMinutes: 15, refreshLifetimeMinutes: 120));
        var user = new AuthUser { Id = 7, Username = "demo", IsOpen = 1 };

        var access = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateToken(user));
        var refresh = new JwtSecurityTokenHandler().ReadJwtToken(service.CreateRefreshToken(user));

        Assert.Equal(15 * 60L, LifetimeSeconds(access));
        Assert.Equal(120 * 60L, LifetimeSeconds(refresh));
    }

    [Fact]
    public void ValidateToken_rejects_a_token_signed_with_another_key()
    {
        var token = new JwtTokenService(TestOptions("first-signing-key-that-is-at-least-32-chars"))
            .CreateToken(new AuthUser { Id = 1, Username = "demo" });

        AssertIdentityModelException(
            () => new JwtTokenService(TestOptions("second-signing-key-that-is-at-least-32-chars")).ValidateToken(token),
            "SecurityTokenSignatureKeyNotFoundException");
    }

    [Fact]
    public void ValidateToken_rejects_an_expired_token()
    {
        var token = TestExpiredHs256Token();

        AssertIdentityModelException(
            () => new JwtTokenService(TestOptions()).ValidateToken(token),
            "SecurityTokenExpiredException");
    }

    [Fact]
    public void ValidateToken_rejects_a_token_using_a_non_hs256_algorithm()
    {
        var token = CreateSignedToken(TestOptions(), "HS512", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10));

        AssertIdentityModelException(
            () => new JwtTokenService(TestOptions()).ValidateToken(token),
            "SecurityTokenSignatureKeyNotFoundException");
    }

    [Fact]
    public void ValidateToken_rejects_a_malformed_token()
    {
        AssertIdentityModelException(
            () => new JwtTokenService(TestOptions()).ValidateToken("not-a-jwt"),
            "SecurityTokenException");
    }

    [Fact]
    public void ValidateToken_rejects_a_token_with_another_issuer()
    {
        var token = CreateSignedToken(TestOptions(), "HS256", DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(10), "another-issuer");

        AssertIdentityModelException(
            () => new JwtTokenService(TestOptions()).ValidateToken(token),
            "SecurityTokenInvalidIssuerException");
    }

    [Fact]
    public void ValidateToken_rejects_a_token_that_is_not_yet_valid()
    {
        var token = CreateSignedToken(
            TestOptions(), "HS256", DateTime.UtcNow.AddMinutes(5), DateTime.UtcNow.AddMinutes(10));

        AssertIdentityModelException(
            () => new JwtTokenService(TestOptions()).ValidateToken(token),
            "SecurityTokenNotYetValidException");
    }

    private static AuthOptions TestOptions(
        string signingKey = "test-signing-key-that-is-at-least-32-chars",
        int accessLifetimeMinutes = 60,
        int refreshLifetimeMinutes = 10080) =>
        AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_JWT_SIGNING_KEY"] = signingKey,
            ["FGC_JWT_ISSUER"] = "synthetic-issuer",
            ["FGC_JWT_EXPIRES_MINUTES"] = accessLifetimeMinutes.ToString(),
            ["FGC_JWT_REFRESH_EXPIRES_MINUTES"] = refreshLifetimeMinutes.ToString()
        });

    private static string TestExpiredHs256Token()
    {
        return CreateSignedToken(
            TestOptions(), "HS256", DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-5));
    }

    private static void AssertIdentityModelException(Action action, string expectedTypeName)
    {
        var exception = Assert.ThrowsAny<Exception>(action);
        var exceptionType = exception.GetType();
        Assert.Equal(expectedTypeName, exceptionType.Name);
        var currentType = exceptionType;
        var isSecurityTokenException = false;
        while (currentType is not null)
        {
            isSecurityTokenException |= currentType.Name == "SecurityTokenException";
            currentType = currentType.BaseType;
        }

        Assert.True(isSecurityTokenException, $"Expected an IdentityModel SecurityTokenException, got {exceptionType.FullName}.");
    }

    private static string CreateSignedToken(
        AuthOptions options,
        string algorithm,
        DateTime notBefore,
        DateTime expires,
        string? issuer = null)
    {
        var header = Base64Url($"{{\"alg\":\"{algorithm}\",\"typ\":\"JWT\"}}");
        var payload = Base64Url(
            $"{{\"sub\":\"1\",\"iss\":\"{issuer ?? options.JwtIssuer}\",\"nbf\":{Epoch(notBefore)},\"exp\":{Epoch(expires)}}}");
        var unsignedToken = $"{header}.{payload}";
        using HMAC hmac = algorithm == "HS512"
            ? new HMACSHA512(System.Text.Encoding.UTF8.GetBytes(options.JwtSigningKey))
            : new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(options.JwtSigningKey));

        return $"{unsignedToken}.{Base64Url(hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(unsignedToken)))}";
    }

    private static long Epoch(DateTime value) => new DateTimeOffset(value).ToUnixTimeSeconds();

    private static string Base64Url(string value) => Base64Url(System.Text.Encoding.UTF8.GetBytes(value));

    private static string Base64Url(byte[] value) => Convert.ToBase64String(value)
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_');

    private static long LifetimeSeconds(JwtSecurityToken token) =>
        long.Parse(token.Claims.Single(claim => claim.Type == "exp").Value) -
        long.Parse(token.Claims.Single(claim => claim.Type == "nbf").Value);
}
