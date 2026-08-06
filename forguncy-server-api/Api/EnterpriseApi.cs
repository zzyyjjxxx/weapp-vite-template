using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;
using GrapeCity.Forguncy.ServerApi;
using Newtonsoft.Json;

namespace ForguncyServerApi.Api;

public class EnterpriseApi : ForguncyApi
{
    private static readonly object EnterpriseFactoryOverrideGate = new();
    private static Func<CancellationToken, Task<EnterpriseCompositionRoot>>? enterpriseFactoryOverrideForTests;

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
                await ApiResponseWriter.WriteJsonAsync(
                    Context.Response,
                    400,
                    new ErrorResponse("invalid_request"),
                    cancellationToken);
                return;
            }

            var enterprise = await GetEnterpriseAsync(cancellationToken);
            var result = await enterprise.LoginAsync(request, cancellationToken);
            var response = CreateLoginResponse(result);
            await ApiResponseWriter.WriteJsonAsync(
                Context.Response,
                response.StatusCode,
                response.Payload,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            RecordUnexpectedLoginFailure(Context.RequestServices, exception);
            await ApiResponseWriter.WriteJsonAsync(
                Context.Response,
                500,
                CreateServerErrorResponse(),
                cancellationToken);
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
                await ApiResponseWriter.WriteJsonAsync(
                    Context.Response,
                    400,
                    new ErrorResponse("invalid_request"),
                    cancellationToken);
                return;
            }

            var enterprise = await GetEnterpriseAsync(cancellationToken);
            var result = await enterprise.RefreshAsync(refreshToken, cancellationToken);
            var response = CreateRefreshResponse(result);
            await ApiResponseWriter.WriteJsonAsync(
                Context.Response,
                response.StatusCode,
                response.Payload,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            RecordUnexpectedRefreshFailure(Context.RequestServices, exception);
            await ApiResponseWriter.WriteJsonAsync(
                Context.Response,
                500,
                CreateServerErrorResponse(),
                cancellationToken);
        }
    }

    [Get]
    public async Task GetInfo()
    {
        var cancellationToken = Context.RequestAborted;
        try
        {
            var enterprise = await GetEnterpriseAsync(cancellationToken);

            EnterpriseIdentity identity;
            try
            {
                identity = await AccessTokenReader.ReadRequiredIdentity(
                    Context.Request,
                    enterprise.Tokens,
                    cancellationToken);
            }
            catch (AccessTokenFormatException)
            {
                var invalidResponse = CreateInvalidAccessTokenResponse();
                await ApiResponseWriter.WriteJsonAsync(
                    Context.Response,
                    invalidResponse.StatusCode,
                    invalidResponse.Payload,
                    cancellationToken);
                return;
            }

            var profile = await enterprise.GetInfoAsync(identity, cancellationToken);
            var response = profile is null
                ? CreateGetInfoNotFoundResponse()
                : CreateGetInfoResponse(profile);

            await ApiResponseWriter.WriteJsonAsync(
                Context.Response,
                response.StatusCode,
                response.Payload,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            RecordUnexpectedGetInfoFailure(Context.RequestServices, exception);
            await ApiResponseWriter.WriteJsonAsync(
                Context.Response,
                500,
                CreateServerErrorResponse(),
                cancellationToken);
        }
    }

    internal static IDisposable PushCompositionRootFactoryOverrideForTests(
        Func<CancellationToken, Task<EnterpriseCompositionRoot>> factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        lock (EnterpriseFactoryOverrideGate)
        {
            var previous = enterpriseFactoryOverrideForTests;
            enterpriseFactoryOverrideForTests = factory;
            return new EnterpriseFactoryOverrideScope(previous);
        }
    }

    private Task<EnterpriseCompositionRoot> GetEnterpriseAsync(CancellationToken cancellationToken)
    {
        Func<CancellationToken, Task<EnterpriseCompositionRoot>>? factoryOverride;
        lock (EnterpriseFactoryOverrideGate)
        {
            factoryOverride = enterpriseFactoryOverrideForTests;
        }

        return factoryOverride is not null
            ? factoryOverride(cancellationToken)
            : EnterpriseCompositionRoot.GetOrCreateAsync(DataAccess, cancellationToken);
    }

    private static void RecordUnexpectedLoginFailure(IServiceProvider? services, Exception exception) =>
        EnterpriseDiagnostics.RecordLogin(services, exception);

    private static void RecordUnexpectedRefreshFailure(IServiceProvider? services, Exception exception) =>
        EnterpriseDiagnostics.RecordRefresh(services, exception);

    private static void RecordUnexpectedGetInfoFailure(IServiceProvider? services, Exception exception) =>
        EnterpriseDiagnostics.RecordGetInfo(services, exception);

    private static ErrorResponse CreateServerErrorResponse() => new("server_error");

    private static ApiResponse CreateInvalidAccessTokenResponse() => new(401, new ErrorResponse("invalid_token"));

    private static ApiResponse CreateGetInfoNotFoundResponse() => new(404, new ErrorResponse("enterprise_not_found"));

    private static ApiResponse CreateGetInfoResponse(EnterpriseProfile profile) =>
        new(
            200,
            new EnterpriseInfoResponse(
                profile.BusinessName,
                profile.CreditCode,
                profile.CountyName));

    private static ApiResponse CreateLoginResponse(LoginResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        return result.Status switch
        {
            LoginStatus.Success => new ApiResponse(200, CreateTokenResponse(result.Tokens!)),
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

    private record ApiResponse(int StatusCode, object Payload);

    private record TokenResponse(
        [property: JsonProperty("access_token")] string AccessToken,
        [property: JsonProperty("refresh_token")] string RefreshToken,
        [property: JsonProperty("token_type")] string TokenType,
        [property: JsonProperty("expires_in")] int ExpiresInSeconds,
        [property: JsonProperty("refresh_expires_in")] int RefreshExpiresInSeconds);

    private record EnterpriseInfoResponse(
        [property: JsonProperty("businessname")] string BusinessName,
        [property: JsonProperty("creditcode")] string CreditCode,
        [property: JsonProperty("county")] string County);

    private record ErrorResponse([property: JsonProperty("error")] string Error);

    private sealed class EnterpriseFactoryOverrideScope : IDisposable
    {
        private readonly Func<CancellationToken, Task<EnterpriseCompositionRoot>>? previous;
        private bool disposed;

        public EnterpriseFactoryOverrideScope(Func<CancellationToken, Task<EnterpriseCompositionRoot>>? previous)
        {
            this.previous = previous;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            lock (EnterpriseFactoryOverrideGate)
            {
                enterpriseFactoryOverrideForTests = previous;
            }

            disposed = true;
        }
    }
}
