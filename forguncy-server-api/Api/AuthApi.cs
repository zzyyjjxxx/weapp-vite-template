using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ForguncyServerApi.Application;
using GrapeCity.Forguncy.ServerApi;
using Microsoft.Extensions.Logging;

namespace ForguncyServerApi.Api;

public sealed class AuthApi : ForguncyApi
{
    private const string UnexpectedLoginOperationCode = "auth.login.unexpected_failure";
    private static readonly EventId UnexpectedLoginEvent = new(1001, "AuthLoginUnexpectedFailure");

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
        catch (Exception exception)
        {
            RecordUnexpectedLoginFailure(Context.RequestServices, exception);
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

    private static void RecordUnexpectedLoginFailure(IServiceProvider? services, Exception exception)
    {
        var exceptionType = exception.GetType().Name;

        try
        {
            var logger = services?.GetService(typeof(ILogger<AuthApi>)) as ILogger
                ?? services?.GetService(typeof(ILogger)) as ILogger;
            if (logger is null && services?.GetService(typeof(ILoggerFactory)) is ILoggerFactory loggerFactory)
            {
                logger = loggerFactory.CreateLogger(typeof(AuthApi).FullName ?? nameof(AuthApi));
            }

            if (logger is not null)
            {
                logger.LogError(
                    UnexpectedLoginEvent,
                    "Operation {OperationCode} failed with exception type {ExceptionType}.",
                    UnexpectedLoginOperationCode,
                    exceptionType);
                return;
            }
        }
        catch (Exception)
        {
            // Diagnostics must never replace the fixed client error response.
        }

        try
        {
            Trace.TraceError(
                "Operation {0} failed with exception type {1}.",
                UnexpectedLoginOperationCode,
                exceptionType);
        }
        catch (Exception)
        {
            // A failing Trace listener must not alter the client response.
        }
    }

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
