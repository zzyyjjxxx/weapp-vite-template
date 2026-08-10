using System.Text;
using ForguncyServerApi.Api;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Security;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class AccessTokenReaderTests
{
    [Fact]
    public async Task ReadRequiredIdentity_reads_a_bearer_access_token_identity()
    {
        var service = TestJwtTokenService();
        var token = service.CreateToken(new AuthUser { Id = 7, Username = "91330200SYNTHETIC" });
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = $"Bearer {token}";

        var identity = await AccessTokenReader.ReadRequiredIdentity(
            context.Request,
            service,
            CancellationToken.None);

        Assert.Equal(7, identity.UserId);
        Assert.Equal("91330200SYNTHETIC", identity.CreditCode);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("Basic synthetic", null)]
    [InlineData("Bearer ", null)]
    [InlineData("Bearer {0}", true)]
    public async Task ReadRequiredIdentity_rejects_invalid_authorization_headers(
        string? authorizationValue,
        bool? useRefreshToken)
    {
        var service = TestJwtTokenService();
        var token = service.CreateToken(new AuthUser { Id = 7, Username = "91330200SYNTHETIC" });
        if (useRefreshToken == true)
        {
            token = service.CreateRefreshToken(new AuthUser { Id = 7, Username = "91330200SYNTHETIC" });
        }

        var context = new DefaultHttpContext();
        if (authorizationValue is not null)
        {
            context.Request.Headers["Authorization"] = string.Format(authorizationValue, token);
        }

        var exception = await Record.ExceptionAsync(
            () => AccessTokenReader.ReadRequiredIdentity(
                context.Request,
                service,
                CancellationToken.None));

        Assert.NotNull(exception);
        Assert.Equal("AccessTokenFormatException", exception.GetType().Name);
        Assert.Equal("The access token format is invalid.", exception.Message);
        Assert.DoesNotContain(token, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteJsonAsync_writes_utf8_json_and_no_store_headers()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await ApiResponseWriter.WriteJsonAsync(
            context.Response,
            StatusCodes.Status401Unauthorized,
            new { error = "invalid_access_token", detail = "企业" },
            CancellationToken.None);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", context.Response.ContentType);
        Assert.Equal("no-store", context.Response.Headers["Cache-Control"].ToString());
        Assert.Equal("no-cache", context.Response.Headers["Pragma"].ToString());

        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8, false, 1024, true);
        var json = await reader.ReadToEndAsync();
        Assert.Equal("{\"error\":\"invalid_access_token\",\"detail\":\"企业\"}", json);
        Assert.Equal(
            Encoding.UTF8.GetBytes(json),
            ((MemoryStream)context.Response.Body).ToArray());
    }

    private static JwtTokenService TestJwtTokenService() => new(TestOptions());

    private static ForguncyServerApi.Configuration.AuthOptions TestOptions() =>
        ForguncyServerApi.Configuration.AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_JWT_SIGNING_KEY"] = "test-signing-key-that-is-at-least-32-chars",
            ["FGC_JWT_ISSUER"] = "synthetic-issuer",
            ["FGC_JWT_EXPIRES_MINUTES"] = "60",
            ["FGC_JWT_REFRESH_EXPIRES_MINUTES"] = "10080"
        });
}
