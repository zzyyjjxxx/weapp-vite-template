using ForguncyServerApi.Application;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using Microsoft.EntityFrameworkCore;

namespace ForguncyServerApi.Api;

public sealed class AuthCompositionRoot
{
    private readonly AuthService authService;

    private AuthCompositionRoot(AuthService authService)
    {
        this.authService = authService;
    }

    public static async Task<AuthCompositionRoot> CreateAsync(CancellationToken cancellationToken)
    {
        var options = AuthOptions.FromEnvironment();
        var dbContextOptions = AuthDbContextOptionsFactory.Create(options);
        Func<AuthDbContext> contextFactory = () => new AuthDbContext(dbContextOptions);
        var initializer = new AuthDbInitializer(contextFactory, options);
        await initializer.EnsureCreatedAndBootstrapAsync(cancellationToken);

        var authService = new AuthService(
            new UserRepository(contextFactory),
            new PasswordHasher(),
            new JwtTokenService(options),
            options.JwtLifetime);
        return new AuthCompositionRoot(authService);
    }

    public Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);
}
