using System.Text.Json;
using System.Text.Json.Serialization;
using ForguncyServerApi.Application;
using Microsoft.AspNetCore.Http;

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
        ArgumentNullException.ThrowIfNull(request);

        if (request.HasJsonContentType())
        {
            return await ReadJsonAsync(request, cancellationToken);
        }

        if (request.HasFormContentType)
        {
            return await ReadFormAsync(request, cancellationToken);
        }

        throw new LoginRequestFormatException();
    }

    private static async Task<LoginRequest> ReadJsonAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await JsonSerializer.DeserializeAsync<LoginRequestPayload>(request.Body, cancellationToken: cancellationToken);
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

    private static LoginRequest ToLoginRequest(string? username, string? password) =>
        username is not null && password is not null
            ? new LoginRequest(username, password)
            : throw new LoginRequestFormatException();

    private sealed record LoginRequestPayload(
        [property: JsonPropertyName("username")] string? Username,
        [property: JsonPropertyName("password")] string? Password);
}
