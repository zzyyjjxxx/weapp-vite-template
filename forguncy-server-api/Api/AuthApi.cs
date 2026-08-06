using System.Text;
using ForguncyServerApi.Application;
using GrapeCity.Forguncy.ServerApi;
using Newtonsoft.Json;
using System.Reflection;

namespace ForguncyServerApi.Api;

public class AuthApi : ForguncyApi
{
    // Forguncy hosts a custom API assembly for one site, so this cache is scoped to that host lifetime.
    private static readonly RetryableAsyncCache<AuthCompositionRoot> AuthCompositionCache = new();

    [Post]
    public async Task Login()
    {
        var cancellationToken = Context.RequestAborted;
        try
        {
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

            var auth = await AuthCompositionCache.GetOrCreateAsync(
                () => AuthCompositionRoot.CreateAsync(DataAccess, CancellationToken.None),
                cancellationToken);
            var result = await auth.LoginAsync(request, cancellationToken);
            var response = CreateLoginResponse(result);
            await WriteJsonAsync(response.StatusCode, response.Payload, cancellationToken);
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
    }

    [Post]
    public async Task Refresh()
    {
        var cancellationToken = Context.RequestAborted;
        try
        {
            string refreshToken;
            try
            {
                refreshToken = await LoginRequestReader.ReadRefreshTokenAsync(Context.Request, cancellationToken);
            }
            catch (LoginRequestFormatException)
            {
                await WriteJsonAsync(400, new ErrorResponse("invalid_request"), cancellationToken);
                return;
            }

            var auth = await AuthCompositionCache.GetOrCreateAsync(
                () => AuthCompositionRoot.CreateAsync(DataAccess, CancellationToken.None),
                cancellationToken);
            var result = await RefreshAsync(auth, refreshToken, cancellationToken);
            var response = CreateRefreshResponse(result);
            await WriteJsonAsync(response.StatusCode, response.Payload, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            RecordUnexpectedRefreshFailure(Context.RequestServices, exception);
            await WriteJsonAsync(500, CreateServerErrorResponse(), cancellationToken);
            return;
        }
    }

    private async Task WriteJsonAsync(int statusCode, object value, CancellationToken cancellationToken)
    {
        Context.Response.StatusCode = statusCode;
        Context.Response.ContentType = "application/json; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        await Context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    private static ErrorResponse CreateServerErrorResponse() => new("server_error");

    private static void RecordUnexpectedLoginFailure(IServiceProvider? services, Exception exception) =>
        AuthDiagnostics.RecordLogin(services, exception);

    private static void RecordUnexpectedRefreshFailure(IServiceProvider? services, Exception exception) =>
        AuthDiagnostics.RecordRefresh(services, exception);

    private static ApiResponse CreateLoginResponse(LoginResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.Status switch
        {
            LoginStatus.Success => new ApiResponse(
                200,
                CreateTokenResponse(result.Tokens!)),
            LoginStatus.InvalidRequest => new ApiResponse(400, new ErrorResponse("invalid_request")),
            LoginStatus.InvalidCredentials => new ApiResponse(401, new ErrorResponse("invalid_credentials")),
            _ => throw new InvalidOperationException("The login result status is not supported.")
        };
    }

    private static ApiResponse CreateRefreshResponse(RefreshResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.Status switch
        {
            RefreshStatus.Success => new ApiResponse(200, CreateTokenResponse(result.Tokens!)),
            RefreshStatus.InvalidRequest => new ApiResponse(400, new ErrorResponse("invalid_request")),
            RefreshStatus.InvalidToken => new ApiResponse(401, new ErrorResponse("invalid_refresh_token")),
            _ => throw new InvalidOperationException("The refresh result status is not supported.")
        };
    }

    private static TokenResponse CreateTokenResponse(TokenPair tokens) =>
        new(
            tokens.AccessToken,
            tokens.RefreshToken,
            "Bearer",
            tokens.ExpiresInSeconds,
            tokens.RefreshExpiresInSeconds);

    private static Task<RefreshResult> RefreshAsync(
        AuthCompositionRoot auth,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (auth is null)
        {
            throw new ArgumentNullException(nameof(auth));
        }

        var authServiceField = typeof(AuthCompositionRoot).GetField(
            "authService",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var authService = authServiceField?.GetValue(auth) as AuthService
            ?? throw new InvalidOperationException("The auth composition root is not initialized.");

        return authService.RefreshAsync(refreshToken, cancellationToken);
    }

    private record ApiResponse(int StatusCode, object Payload);

    private record TokenResponse(
        [property: JsonProperty("access_token")] string AccessToken,
        [property: JsonProperty("refresh_token")] string RefreshToken,
        [property: JsonProperty("token_type")] string TokenType,
        [property: JsonProperty("expires_in")] int ExpiresInSeconds,
        [property: JsonProperty("refresh_expires_in")] int RefreshExpiresInSeconds);

    private record ErrorResponse([property: JsonProperty("error")] string Error);
}
