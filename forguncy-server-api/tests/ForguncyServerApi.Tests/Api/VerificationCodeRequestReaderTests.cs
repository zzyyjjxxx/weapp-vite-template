using System.Text;
using ForguncyServerApi.Api;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ForguncyServerApi.Tests.Api;

public sealed class VerificationCodeRequestReaderTests
{
    [Fact]
    public async Task ReadSendAsync_reads_mobile_from_json()
    {
        var context = CreateContext(
            "application/json",
            "{\"mobile\":\"13800000000\"}");

        var request = await VerificationCodeRequestReader.ReadSendAsync(
            context.Request,
            CancellationToken.None);

        Assert.Equal("13800000000", request.Mobile);
    }

    [Fact]
    public async Task ReadVerifyAsync_reads_mobile_and_code_from_form_data()
    {
        var context = CreateContext(
            "application/x-www-form-urlencoded",
            "mobile=13800000000&code=123456");

        var request = await VerificationCodeRequestReader.ReadVerifyAsync(
            context.Request,
            CancellationToken.None);

        Assert.Equal("13800000000", request.Mobile);
        Assert.Equal("123456", request.Code);
    }

    [Theory]
    [InlineData("{\"mobile\":42}")]
    [InlineData("{}")]
    [InlineData("mobile=13800000000")]
    public async Task ReadVerifyAsync_rejects_missing_or_non_string_code(string body)
    {
        var contentType = body.StartsWith("{", StringComparison.Ordinal)
            ? "application/json"
            : "application/x-www-form-urlencoded";
        var context = CreateContext(contentType, body);

        await Assert.ThrowsAsync<VerificationCodeRequestFormatException>(
            () => VerificationCodeRequestReader.ReadVerifyAsync(
                context.Request,
                CancellationToken.None));
    }

    [Fact]
    public async Task ReadSendAsync_rejects_multipart_data()
    {
        var context = CreateContext(
            "multipart/form-data; boundary=synthetic",
            "--synthetic--");

        await Assert.ThrowsAsync<VerificationCodeRequestFormatException>(
            () => VerificationCodeRequestReader.ReadSendAsync(
                context.Request,
                CancellationToken.None));
    }

    private static DefaultHttpContext CreateContext(string contentType, string body)
    {
        var context = new DefaultHttpContext();
        context.Request.ContentType = contentType;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        return context;
    }
}
