using System.Text;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ForguncyServerApi.Api;

public sealed class VerificationCodeRequestFormatException : Exception
{
    public VerificationCodeRequestFormatException()
        : base("The verification code request format is invalid.")
    {
    }
}

public sealed record SendVerificationCodeRequest(string Mobile);

public sealed record VerifyVerificationCodeRequest(string Mobile, string Code);

public static class VerificationCodeRequestReader
{
    public static async Task<SendVerificationCodeRequest> ReadSendAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (IsJsonContentType(request))
        {
            return await ReadSendJsonAsync(request, cancellationToken);
        }

        if (IsUrlEncodedForm(request))
        {
            var form = await ReadFormAsync(request, cancellationToken);
            return form.ContainsKey("mobile")
                ? new SendVerificationCodeRequest(form["mobile"].ToString())
                : throw new VerificationCodeRequestFormatException();
        }

        throw new VerificationCodeRequestFormatException();
    }

    public static async Task<VerifyVerificationCodeRequest> ReadVerifyAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (IsJsonContentType(request))
        {
            return await ReadVerifyJsonAsync(request, cancellationToken);
        }

        if (IsUrlEncodedForm(request))
        {
            var form = await ReadFormAsync(request, cancellationToken);
            return form.ContainsKey("mobile") && form.ContainsKey("code")
                ? new VerifyVerificationCodeRequest(
                    form["mobile"].ToString(),
                    form["code"].ToString())
                : throw new VerificationCodeRequestFormatException();
        }

        throw new VerificationCodeRequestFormatException();
    }

    private static async Task<SendVerificationCodeRequest> ReadSendJsonAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var payload = await ReadJsonAsync(request, cancellationToken);
        return new SendVerificationCodeRequest(ReadRequiredString(payload, "mobile"));
    }

    private static async Task<VerifyVerificationCodeRequest> ReadVerifyJsonAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        var payload = await ReadJsonAsync(request, cancellationToken);
        return new VerifyVerificationCodeRequest(
            ReadRequiredString(payload, "mobile"),
            ReadRequiredString(payload, "code"));
    }

    private static async Task<JObject> ReadJsonAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true);
            var json = await reader.ReadToEndAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return JObject.Parse(json);
        }
        catch (JsonException)
        {
            throw new VerificationCodeRequestFormatException();
        }
    }

    private static async Task<IFormCollection> ReadFormAsync(
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            throw new VerificationCodeRequestFormatException();
        }
    }

    private static string ReadRequiredString(JObject payload, string name)
    {
        var token = payload[name];
        return token?.Type == JTokenType.String
            ? token.Value<string>()!
            : throw new VerificationCodeRequestFormatException();
    }

    private static bool IsUrlEncodedForm(HttpRequest request) =>
        string.Equals(GetMediaType(request.ContentType), "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);

    private static bool IsJsonContentType(HttpRequest request)
    {
        var mediaType = GetMediaType(request.ContentType);
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            || (mediaType?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string? GetMediaType(string? contentType) =>
        contentType?.Split(new[] { ';' }, 2)[0].Trim();
}
