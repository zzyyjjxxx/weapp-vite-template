using System.Text;
using ForguncyServerApi.Api;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class LoginRequestReaderTests
{
    [Fact]
    public async Task ReadAsync_reads_username_and_password_from_json()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = Body("{ \"username\": \"demo\", \"password\": \"demo123\" }");

        var request = await LoginRequestReader.ReadAsync(context.Request, CancellationToken.None);

        Assert.Equal("demo", request.Username);
        Assert.Equal("demo123", request.Password);
    }

    [Fact]
    public async Task ReadAsync_reads_username_and_password_from_form_fields()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = Body("username=demo&password=demo123");

        var request = await LoginRequestReader.ReadAsync(context.Request, CancellationToken.None);

        Assert.Equal("demo", request.Username);
        Assert.Equal("demo123", request.Password);
    }

    [Fact]
    public async Task ReadRefreshTokenAsync_reads_a_refresh_token_from_json()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = Body("{ \"refresh_token\": \"refresh-token\" }");

        var refreshToken = await LoginRequestReader.ReadRefreshTokenAsync(context.Request, CancellationToken.None);

        Assert.Equal("refresh-token", refreshToken);
    }

    [Fact]
    public async Task ReadRefreshTokenAsync_reads_a_refresh_token_from_form_fields()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Body = Body("refresh_token=refresh-token");

        var refreshToken = await LoginRequestReader.ReadRefreshTokenAsync(context.Request, CancellationToken.None);

        Assert.Equal("refresh-token", refreshToken);
    }

    [Fact]
    public async Task ReadAsync_rejects_multipart_form_data()
    {
        const string boundary = "----synthetic-boundary";
        var context = new DefaultHttpContext();
        context.Request.ContentType = $"multipart/form-data; boundary={boundary}";
        context.Request.Body = Body(
            $"--{boundary}\r\n" +
            "Content-Disposition: form-data; name=\"username\"\r\n\r\n" +
            "demo\r\n" +
            $"--{boundary}\r\n" +
            "Content-Disposition: form-data; name=\"password\"\r\n\r\n" +
            "demo123\r\n" +
            $"--{boundary}--\r\n");

        await Assert.ThrowsAsync<LoginRequestFormatException>(
            () => LoginRequestReader.ReadAsync(context.Request, CancellationToken.None));
    }

    [Fact]
    public async Task ReadRefreshTokenAsync_rejects_multipart_form_data()
    {
        const string boundary = "----synthetic-boundary";
        var context = new DefaultHttpContext();
        context.Request.ContentType = $"multipart/form-data; boundary={boundary}";
        context.Request.Body = Body(
            $"--{boundary}\r\n" +
            "Content-Disposition: form-data; name=\"refresh_token\"\r\n\r\n" +
            "refresh-token\r\n" +
            $"--{boundary}--\r\n");

        await Assert.ThrowsAsync<LoginRequestFormatException>(
            () => LoginRequestReader.ReadRefreshTokenAsync(context.Request, CancellationToken.None));
    }

    [Fact]
    public async Task ReadAsync_raises_a_format_exception_for_malformed_json()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = Body("{ not-json }");

        await Assert.ThrowsAsync<LoginRequestFormatException>(
            () => LoginRequestReader.ReadAsync(context.Request, CancellationToken.None));
    }

    [Fact]
    public async Task ReadRefreshTokenAsync_raises_a_format_exception_for_malformed_json()
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/json";
        context.Request.Body = Body("{ not-json }");

        await Assert.ThrowsAsync<LoginRequestFormatException>(
            () => LoginRequestReader.ReadRefreshTokenAsync(context.Request, CancellationToken.None));
    }

    [Theory]
    [InlineData("{ \"username\": \"demo\" }")]
    [InlineData("username=demo")]
    public async Task ReadAsync_raises_a_format_exception_for_missing_fields(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = body.StartsWith("{", StringComparison.Ordinal)
            ? "application/json"
            : "application/x-www-form-urlencoded";
        context.Request.Body = Body(body);

        await Assert.ThrowsAsync<LoginRequestFormatException>(
            () => LoginRequestReader.ReadAsync(context.Request, CancellationToken.None));
    }

    [Theory]
    [InlineData("{ \"username\": \"demo\" }")]
    [InlineData("username=demo")]
    [InlineData("{ }")]
    [InlineData("")]
    public async Task ReadRefreshTokenAsync_raises_a_format_exception_for_missing_fields(string body)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = body.StartsWith("{", StringComparison.Ordinal)
            ? "application/json"
            : "application/x-www-form-urlencoded";
        context.Request.Body = Body(body);

        await Assert.ThrowsAsync<LoginRequestFormatException>(
            () => LoginRequestReader.ReadRefreshTokenAsync(context.Request, CancellationToken.None));
    }

    private static MemoryStream Body(string value) => new(Encoding.UTF8.GetBytes(value));
}
