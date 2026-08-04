using ForguncyServerApi.Application;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using GrapeCity.Forguncy.ServerApi;
using Microsoft.EntityFrameworkCore;

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
        var options = AuthOptions.FromEnvironment();
        var dbContextOptions = AuthDbContextOptionsFactory.Create(connectionString);
        Func<AuthDbContext> contextFactory = () => new AuthDbContext(dbContextOptions);

        var authService = new AuthService(
            new UserRepository(contextFactory),
            new PasswordHasher(),
            new JwtTokenService(options),
            options.JwtLifetime);
        return Task.FromResult(new AuthCompositionRoot(authService));
    }

    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);
}
