using System.Text;
using ForguncyServerApi.Application;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ForguncyServerApi.Api;

public sealed class LoginRequestFormatException : Exception
{
    public LoginRequestFormatException()
        : base("The login request format is invalid.")
    {
    }
}

public static class LoginRequestReader
{
    public static async Task<LoginRequest> ReadAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (IsJsonContentType(request))
        {
            return await ReadJsonAsync(request, cancellationToken);
        }

        if (IsUrlEncodedForm(request))
        {
            return await ReadFormAsync(request, cancellationToken);
        }

        throw new LoginRequestFormatException();
    }

    public static async Task<string> ReadRefreshTokenAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (IsJsonContentType(request))
        {
            return await ReadRefreshTokenJsonAsync(request, cancellationToken);
        }

        if (IsUrlEncodedForm(request))
        {
            return await ReadRefreshTokenFormAsync(request, cancellationToken);
        }

        throw new LoginRequestFormatException();
    }

    private static async Task<LoginRequest> ReadJsonAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true);
            var json = await reader.ReadToEndAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var payload = JsonConvert.DeserializeObject<LoginRequestPayload>(json);
            return ToLoginRequest(payload?.Username, payload?.Password);
        }
        catch (JsonException)
        {
            throw new LoginRequestFormatException();
        }
    }

    private static async Task<LoginRequest> ReadFormAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await request.ReadFormAsync(cancellationToken);
            return form.ContainsKey("username") && form.ContainsKey("password")
                ? new LoginRequest(form["username"].ToString(), form["password"].ToString())
                : throw new LoginRequestFormatException();
        }
        catch (InvalidDataException)
        {
            throw new LoginRequestFormatException();
        }
    }

    private static async Task<string> ReadRefreshTokenJsonAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var reader = new StreamReader(request.Body, Encoding.UTF8, true, 1024, true);
            var json = await reader.ReadToEndAsync();
            cancellationToken.ThrowIfCancellationRequested();
            var payload = JsonConvert.DeserializeObject<RefreshRequestPayload>(json);
            return payload?.RefreshToken is not null
                ? payload.RefreshToken
                : throw new LoginRequestFormatException();
        }
        catch (JsonException)
        {
            throw new LoginRequestFormatException();
        }
    }

    private static async Task<string> ReadRefreshTokenFormAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var form = await request.ReadFormAsync(cancellationToken);
            return form.ContainsKey("refresh_token")
                ? form["refresh_token"].ToString()
                : throw new LoginRequestFormatException();
        }
        catch (InvalidDataException)
        {
            throw new LoginRequestFormatException();
        }
    }

    private static LoginRequest ToLoginRequest(string? username, string? password) =>
        username is not null && password is not null
            ? new LoginRequest(username, password)
            : throw new LoginRequestFormatException();

    private static bool IsUrlEncodedForm(HttpRequest request)
    {
        var mediaType = GetMediaType(request.ContentType);
        return string.Equals(mediaType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsJsonContentType(HttpRequest request)
    {
        var mediaType = GetMediaType(request.ContentType);
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            || (mediaType?.EndsWith("+json", StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static string? GetMediaType(string? contentType) =>
        contentType?.Split(new[] { ';' }, 2)[0].Trim();

    private sealed record LoginRequestPayload(
        [property: JsonProperty("username")] string? Username,
        [property: JsonProperty("password")] string? Password);

    private sealed record RefreshRequestPayload(
        [property: JsonProperty("refresh_token")] string? RefreshToken);
}
