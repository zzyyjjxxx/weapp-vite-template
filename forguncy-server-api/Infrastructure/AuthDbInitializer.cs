using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Security;
using Microsoft.EntityFrameworkCore;

namespace ForguncyServerApi.Infrastructure;

public sealed class AuthDbInitializer
{
    private static readonly SemaphoreSlim InitializationGate = new(1, 1);
    private readonly Func<AuthDbContext> _contextFactory;
    private readonly AuthOptions _options;

    public AuthDbInitializer(Func<AuthDbContext> contextFactory, AuthOptions options)
    {
        _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task EnsureCreatedAndBootstrapAsync(CancellationToken cancellationToken)
    {
        await InitializationGate.WaitAsync(cancellationToken);
        try
        {
            await using var context = _contextFactory();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(_options.BootstrapUsername) ||
                string.IsNullOrWhiteSpace(_options.BootstrapPassword) ||
                await context.Users.AnyAsync(user => user.Username == _options.BootstrapUsername, cancellationToken))
            {
                return;
            }

            var now = DateTime.UtcNow;
            await context.Users.AddAsync(new AuthUser
            {
                Username = _options.BootstrapUsername,
                PasswordHash = new PasswordHasher().Hash(_options.BootstrapPassword),
                IsEnabled = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            }, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            InitializationGate.Release();
        }
    }
}
