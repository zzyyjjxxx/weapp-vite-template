using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using ForguncyServerApi.Application;
using ForguncyServerApi.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;

namespace ForguncyServerApi.Tests.Infrastructure;

public sealed class SmsHttpClientsTests
{
    [Fact]
    public async Task AuthenticationClient_posts_the_three_config_values_and_reads_the_token()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Json("{\"code\":200,\"success\":true,\"message\":\"success\",\"data\":\"mock-token\"}")
        });
        using var httpClient = new HttpClient(handler);
        var client = new HttpSmsAuthenticationClient(httpClient, "https://sms.test/public/auth");

        var result = await client.AuthenticateAsync(
            new SmsAuthenticationRequest("client-id", "client-secret", "tenant-a"),
            CancellationToken.None);

        Assert.Equal(200, result.Code);
        Assert.True(result.Success);
        Assert.Equal("success", result.Message);
        Assert.Equal("mock-token", result.Data);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://sms.test/public/auth", handler.RequestUri?.ToString());

        var body = JObject.Parse(handler.Body!);
        Assert.Equal("client-id", body["client_id"]?.Value<string>());
        Assert.Equal("client-secret", body["client_secret"]?.Value<string>());
        Assert.Equal("tenant-a", body["tenant"]?.Value<string>());
    }

    [Fact]
    public async Task SmsGateway_posts_the_message_and_headers_and_reads_ret_message()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Json(
                "{\"code\":200,\"message\":\"success\",\"data\":{" +
                "\"transactionID\":\"msg-2026080748-000000001\",\"retCode\":\"0000\"," +
                "\"retMsg\":\"调用成功!\"},\"timestamp\":1786084131713,\"executeTime\":null}")
        });
        using var httpClient = new HttpClient(handler);
        var client = new HttpSmsGateway(httpClient, "https://sms.test/service/indata/nx/duanxinsend");

        var result = await client.SendAsync(
            new SmsSendRequest(
                "13800000000",
                "您好，您的用地需求填报验证码是：【123456】",
                "msg-2026080748-000000001",
                "sms-code",
                "sms-secret",
                "mock-token"),
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("success", result.Message);
        Assert.Equal("调用成功!", result.RetMessage);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal(
            "https://sms.test/service/indata/nx/duanxinsend",
            handler.RequestUri?.ToString());
        Assert.Equal("sms-code", handler.Header("zzqscode"));
        Assert.Equal("sms-secret", handler.Header("zzqssecret"));
        Assert.Equal("mock-token", handler.Header("Auth-Token"));

        var body = JObject.Parse(handler.Body!);
        Assert.Equal("13800000000", body["mobile"]?.Value<string>());
        Assert.Equal("您好，您的用地需求填报验证码是：【123456】", body["content"]?.Value<string>());
        Assert.Equal("msg-2026080748-000000001", body["transactionID"]?.Value<string>());
    }

    [Fact]
    public async Task SmsGateway_maps_a_non_success_ret_code_to_the_response_message()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = Json(
                "{\"code\":200,\"message\":\"上游失败\",\"data\":{" +
                "\"retCode\":\"1001\",\"retMsg\":\"号码无效\"}}")
        });
        using var httpClient = new HttpClient(handler);
        var client = new HttpSmsGateway(httpClient, "https://sms.test/send");

        var result = await client.SendAsync(
            new SmsSendRequest("13800000000", "content", "transaction", "code", "secret", "token"),
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("上游失败", result.Message);
        Assert.Equal("号码无效", result.RetMessage);
    }

    private static StringContent Json(string body) =>
        new(body, System.Text.Encoding.UTF8, "application/json");

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            this.responseFactory = responseFactory;
        }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        public string? Body { get; private set; }

        public HttpRequestHeaders Headers { get; private set; } = new HttpRequestMessage().Headers;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Method = request.Method;
            RequestUri = request.RequestUri;
            Body = request.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            Headers = new HttpRequestMessage().Headers;
            foreach (var header in request.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return Task.FromResult(responseFactory(request));
        }

        public string? Header(string name) =>
            Headers.TryGetValues(name, out var values) ? values.Single() : null;
    }
}
