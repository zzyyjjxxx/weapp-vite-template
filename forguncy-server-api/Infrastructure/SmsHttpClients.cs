using System.Text;
using System.Net.Http;
using ForguncyServerApi.Application;
using Newtonsoft.Json;

namespace ForguncyServerApi.Infrastructure;

public static class SmsServiceEndpoints
{
    public const string Authentication = "http://10.74.226.56:9099/public/auth";
    public const string Send = "http://10.74.226.56:9099/service/indata/nx/duanxinsend";
}

public sealed class HttpSmsAuthenticationClient : ISmsAuthenticationClient
{
    private readonly HttpClient httpClient;
    private readonly Uri endpoint;

    public HttpSmsAuthenticationClient(
        HttpClient httpClient,
        string endpoint = SmsServiceEndpoints.Authentication)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.endpoint = new Uri(endpoint ?? throw new ArgumentNullException(nameof(endpoint)));
    }

    public async Task<SmsAuthenticationResult> AuthenticateAsync(
        SmsAuthenticationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    client_id = request.ClientId,
                    client_secret = request.ClientSecret,
                    tenant = request.Tenant
                }),
                Encoding.UTF8,
                "application/json");
            using var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (!response.IsSuccessStatusCode)
            {
                return new(0, false, "SMS authentication HTTP request failed.", null);
            }

            var envelope = JsonConvert.DeserializeObject<AuthenticationEnvelope>(body);
            return envelope is null
                ? new(0, false, "SMS authentication response is invalid.", null)
                : new(envelope.Code, envelope.Success, envelope.Message ?? string.Empty, envelope.Data);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(0, false, "SMS authentication request failed.", null);
        }
    }

    private sealed class AuthenticationEnvelope
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("data")]
        public string? Data { get; set; }
    }
}

public sealed class HttpSmsGateway : ISmsGateway
{
    private readonly HttpClient httpClient;
    private readonly Uri endpoint;

    public HttpSmsGateway(
        HttpClient httpClient,
        string endpoint = SmsServiceEndpoints.Send)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.endpoint = new Uri(endpoint ?? throw new ArgumentNullException(nameof(endpoint)));
    }

    public async Task<SmsSendResult> SendAsync(
        SmsSendRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
            message.Headers.TryAddWithoutValidation("zzqscode", request.Zzqscode);
            message.Headers.TryAddWithoutValidation("zzqssecret", request.Zzqssecret);
            message.Headers.TryAddWithoutValidation("Auth-Token", request.AuthToken);
            message.Content = new StringContent(
                JsonConvert.SerializeObject(new
                {
                    mobile = request.Mobile,
                    content = request.Content,
                    transactionID = request.TransactionId
                }),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync();
            cancellationToken.ThrowIfCancellationRequested();
            if (!response.IsSuccessStatusCode)
            {
                return new(false, "短信服务请求失败", string.Empty);
            }

            var envelope = JsonConvert.DeserializeObject<SmsEnvelope>(body);
            if (envelope is null)
            {
                return new(false, "短信服务响应无效", string.Empty);
            }

            var data = envelope.Data;
            var success = envelope.Code == 200
                && string.Equals(data?.RetCode, "0000", StringComparison.Ordinal);
            if (success)
            {
                return new(true, envelope.Message ?? string.Empty, data!.RetMsg ?? string.Empty);
            }

            return new(
                false,
                string.IsNullOrWhiteSpace(envelope.Message)
                    ? data?.RetMsg ?? "短信发送失败"
                    : envelope.Message!,
                data?.RetMsg ?? string.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new(false, "短信发送请求失败", string.Empty);
        }
    }

    private sealed class SmsEnvelope
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string? Message { get; set; }

        [JsonProperty("data")]
        public SmsData? Data { get; set; }
    }

    private sealed class SmsData
    {
        [JsonProperty("transactionID")]
        public string? TransactionId { get; set; }

        [JsonProperty("retCode")]
        public string? RetCode { get; set; }

        [JsonProperty("retMsg")]
        public string? RetMsg { get; set; }
    }
}
