using System.Text.Json;
using System.Text.Json.Serialization;
using ForguncyServerApi.Application;
using GrapeCity.Forguncy.ServerApi;

namespace ForguncyServerApi.Api;

public sealed class AuthApi : ForguncyApi
{
    [Post]
    public async Task Login()
    {
        var cancellationToken = Context.RequestAborted;
        LoginRequest request;
        try
        {
            request = await LoginRequestReader.ReadAsync(Context.Request, cancellationToken);
        }
        catch (LoginRequestFormatException)
        {
            await WriteJsonAsync(400, new ErrorResponse("invalid_request"), cancellationToken);
            return;
        }

        LoginResult result;
        try
        {
            var auth = await AuthCompositionRoot.CreateAsync(cancellationToken);
            result = await auth.LoginAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            await WriteJsonAsync(500, CreateServerErrorResponse(), cancellationToken);
            return;
        }

        var response = CreateLoginResponse(result);
        await WriteJsonAsync(response.StatusCode, response.Payload, cancellationToken);
    }

    private async Task WriteJsonAsync(int statusCode, object value, CancellationToken cancellationToken)
    {
        Context.Response.StatusCode = statusCode;
        Context.Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(Context.Response.Body, value, cancellationToken: cancellationToken);
    }

    private static ErrorResponse CreateServerErrorResponse() => new("server_error");

    private static ApiResponse CreateLoginResponse(LoginResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch
        {
            LoginStatus.Success => new ApiResponse(
                200,
                new LoginResponse(
                    result.AccessToken!,
                    "Bearer",
                    result.ExpiresInSeconds,
                    new LoginUserResponse(result.User!.Id, result.User.Username))),
            LoginStatus.InvalidRequest => new ApiResponse(400, new ErrorResponse("invalid_request")),
            LoginStatus.InvalidCredentials => new ApiResponse(401, new ErrorResponse("invalid_credentials")),
            _ => throw new InvalidOperationException("The login result status is not supported.")
        };
    }

    private sealed record ApiResponse(int StatusCode, object Payload);

    private sealed record LoginResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("token_type")] string TokenType,
        [property: JsonPropertyName("expires_in")] int ExpiresInSeconds,
        [property: JsonPropertyName("user")] LoginUserResponse User);

    private sealed record LoginUserResponse(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("username")] string Username);

    private sealed record ErrorResponse([property: JsonPropertyName("error")] string Error);
}
