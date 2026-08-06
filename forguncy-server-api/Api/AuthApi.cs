using System.Text;
using ForguncyServerApi.Application;
using GrapeCity.Forguncy.ServerApi;
using Newtonsoft.Json;

namespace ForguncyServerApi.Api;

public class AuthApi : ForguncyApi
{
    private const string UnexpectedLoginOperationCode = "auth.login.unexpected_failure";
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

    private async Task WriteJsonAsync(int statusCode, object value, CancellationToken cancellationToken)
    {
        Context.Response.StatusCode = statusCode;
        Context.Response.ContentType = "application/json; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        await Context.Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    private static ErrorResponse CreateServerErrorResponse() => new("server_error");

    private static void RecordUnexpectedLoginFailure(IServiceProvider? services, Exception exception) =>
        AuthDiagnostics.Record(services, exception);

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
                new LoginResponse(
                    result.AccessToken!,
                    "Bearer",
                    result.ExpiresInSeconds)),
            LoginStatus.InvalidRequest => new ApiResponse(400, new ErrorResponse("invalid_request")),
            LoginStatus.InvalidCredentials => new ApiResponse(401, new ErrorResponse("invalid_credentials")),
            _ => throw new InvalidOperationException("The login result status is not supported.")
        };
    }

    private record ApiResponse(int StatusCode, object Payload);

    private record LoginResponse(
        [property: JsonProperty("access_token")] string AccessToken,
        [property: JsonProperty("token_type")] string TokenType,
        [property: JsonProperty("expires_in")] int ExpiresInSeconds);

    private record ErrorResponse([property: JsonProperty("error")] string Error);
}
