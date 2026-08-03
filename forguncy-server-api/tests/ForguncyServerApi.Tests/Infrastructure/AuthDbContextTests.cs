using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ForguncyServerApi.Tests.Infrastructure;

public sealed class AuthDbContextTests
{
    [Fact]
    public async Task EnsureCreatedAsync_creates_the_jwt_users_table()
    {
        await using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);

        await context.Database.EnsureCreatedAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'jwt_users';";
        var tableName = await command.ExecuteScalarAsync();

        Assert.Equal("jwt_users", tableName);
    }

    [Fact]
    public async Task FindByUsernameAsync_returns_the_user_with_the_exact_username()
    {
        await using var connection = CreateOpenConnection();
        await using var setupContext = CreateContext(connection);
        await setupContext.Database.EnsureCreatedAsync();
        setupContext.Users.Add(CreateUser("Alice"));
        await setupContext.SaveChangesAsync();

        var repository = new UserRepository(() => CreateContext(connection));

        var user = await repository.FindByUsernameAsync("Alice", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal("Alice", user.Username);
        Assert.Null(await repository.FindByUsernameAsync("alice", CancellationToken.None));
    }

    [Fact]
    public async Task FindByUsernameAsync_returns_disabled_users_as_disabled()
    {
        await using var connection = CreateOpenConnection();
        await using var setupContext = CreateContext(connection);
        await setupContext.Database.EnsureCreatedAsync();
        setupContext.Users.Add(CreateUser("disabled-user", isEnabled: false));
        await setupContext.SaveChangesAsync();

        var user = await new UserRepository(() => CreateContext(connection))
            .FindByUsernameAsync("disabled-user", CancellationToken.None);

        Assert.NotNull(user);
        Assert.False(user.IsEnabled);
    }

    [Fact]
    public async Task EnsureCreatedAndBootstrapAsync_inserts_one_hashed_enabled_user_without_overwriting_an_existing_username()
    {
        await using var connection = CreateOpenConnection();
        var options = CreateOptions();
        var initializer = new AuthDbInitializer(() => CreateContext(connection), options);

        await initializer.EnsureCreatedAndBootstrapAsync(CancellationToken.None);
        await initializer.EnsureCreatedAndBootstrapAsync(CancellationToken.None);

        await using var verificationContext = CreateContext(connection);
        var users = await verificationContext.Users.Where(user => user.Username == "demo").ToListAsync();
        var stored = Assert.Single(users);
        Assert.True(stored.IsEnabled);
        Assert.NotEqual("demo123", stored.PasswordHash);
        Assert.True(new PasswordHasher().Verify("demo123", stored.PasswordHash));

        stored.PasswordHash = "existing-hash";
        stored.IsEnabled = false;
        await verificationContext.SaveChangesAsync();

        await initializer.EnsureCreatedAndBootstrapAsync(CancellationToken.None);

        await using var unchangedContext = CreateContext(connection);
        var unchanged = await unchangedContext.Users.SingleAsync(user => user.Username == "demo");
        Assert.Equal("existing-hash", unchanged.PasswordHash);
        Assert.False(unchanged.IsEnabled);
    }

    [Fact]
    public async Task EnsureCreatedAndBootstrapAsync_serializes_concurrent_first_requests()
    {
        await using var connection = CreateOpenConnection();
        var options = CreateOptions();
        var initializer = new AuthDbInitializer(() => CreateContext(connection), options);

        await Task.WhenAll(
            initializer.EnsureCreatedAndBootstrapAsync(CancellationToken.None),
            initializer.EnsureCreatedAndBootstrapAsync(CancellationToken.None));

        await using var verificationContext = CreateContext(connection);
        Assert.Equal(1, await verificationContext.Users.CountAsync(user => user.Username == "demo"));
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static AuthDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite(connection)
            .Options;
        return new AuthDbContext(options);
    }

    private static AuthOptions CreateOptions() => new(
        "Server=synthetic;Database=synthetic;User Id=synthetic;",
        "synthetic-signing-key-that-is-at-least-thirty-two-characters",
        "forguncy-test",
        TimeSpan.FromMinutes(60),
        "demo",
        "demo123");

    private static AuthUser CreateUser(string username, bool isEnabled = true) => new()
    {
        Username = username,
        PasswordHash = "synthetic-hash",
        IsEnabled = isEnabled,
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow
    };
}
