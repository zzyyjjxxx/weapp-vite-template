using ForguncyServerApi.Application;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using GrapeCity.Forguncy.ServerApi;
using SqlSugar;

namespace ForguncyServerApi.Api;

public sealed class AuthCompositionRoot
{
    private readonly AuthService authService;

    private AuthCompositionRoot(AuthService authService)
    {
        this.authService = authService;
    }

    public static Task<AuthCompositionRoot> CreateAsync(IDataAccess dataAccess, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var connectionString = ForguncyConfigConnectionStringReader.ReadRequired(dataAccess);
        var options = AuthOptions.From(ForguncyJwtConfigurationReader.ReadOrCreate(dataAccess));
        Func<SqlSugarClient> clientFactory = () => AuthSqlSugarClientFactory.Create(connectionString);

        var authService = new AuthService(
            new UserRepository(clientFactory),
            new PasswordHasher(),
            new JwtTokenService(options),
            options.JwtLifetime,
            options.JwtRefreshLifetime);
        return Task.FromResult(new AuthCompositionRoot(authService));
    }

    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    public Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
        authService.RefreshAsync(refreshToken, cancellationToken);
}
