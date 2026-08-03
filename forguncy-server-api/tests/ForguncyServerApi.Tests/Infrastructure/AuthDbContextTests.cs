using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace ForguncyServerApi.Tests.Infrastructure;

public sealed class AuthDbContextTests
{
    [Fact]
    public void MySql_model_matches_the_jwt_users_schema_contract()
    {
        using var context = CreateMySqlModelContext();

        var user = Assert.IsAssignableFrom<IReadOnlyEntityType>(
            context.Model.FindEntityType(typeof(AuthUser)));
        Assert.Equal("jwt_users", user.GetTableName());
        Assert.Equal("utf8mb4", user.FindAnnotation("MySql:CharSet")?.Value);
        Assert.Equal("utf8mb4_unicode_ci", user.FindAnnotation("Relational:Collation")?.Value);

        var table = StoreObjectIdentifier.Table("jwt_users", schema: null);
        AssertColumn(user, table, nameof(AuthUser.Id), "id", "BIGINT");
        AssertColumn(user, table, nameof(AuthUser.Username), "username", "varchar(100)");
        AssertColumn(user, table, nameof(AuthUser.PasswordHash), "password_hash", "varchar(512)");
        AssertColumn(user, table, nameof(AuthUser.IsEnabled), "is_enabled", "tinyint(1)");
        AssertColumn(user, table, nameof(AuthUser.CreatedAtUtc), "created_at", "datetime(6)");
        AssertColumn(user, table, nameof(AuthUser.UpdatedAtUtc), "updated_at", "datetime(6)");

        var id = Assert.IsAssignableFrom<IReadOnlyProperty>(user.FindProperty(nameof(AuthUser.Id)));
        Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);

        var username = Assert.IsAssignableFrom<IReadOnlyProperty>(user.FindProperty(nameof(AuthUser.Username)));
        Assert.Equal(100, username.GetMaxLength());
        var usernameIndex = Assert.Single(user.GetIndexes(), index =>
            index.Properties.Count == 1 && index.Properties[0].Name == nameof(AuthUser.Username));
        Assert.True(usernameIndex.IsUnique);

        var passwordHash = Assert.IsAssignableFrom<IReadOnlyProperty>(
            user.FindProperty(nameof(AuthUser.PasswordHash)));
        Assert.Equal(512, passwordHash.GetMaxLength());

        var isEnabled = Assert.IsAssignableFrom<IReadOnlyProperty>(
            user.FindProperty(nameof(AuthUser.IsEnabled)));
        Assert.Equal(true, isEnabled.GetDefaultValue());
    }

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
    public async Task EnsureCreatedAsync_gives_is_enabled_a_default_of_one()
    {
        await using var connection = CreateOpenConnection();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        await using var insert = connection.CreateCommand();
        insert.CommandText = @"
            INSERT INTO jwt_users (username, password_hash, created_at, updated_at)
            VALUES ('default-enabled', 'synthetic-hash', CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);";
        await insert.ExecuteNonQueryAsync();

        await using var select = connection.CreateCommand();
        select.CommandText = "SELECT is_enabled FROM jwt_users WHERE username = 'default-enabled';";

        Assert.Equal(1L, await select.ExecuteScalarAsync());
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
    public async Task FindByUsernameAsync_rejects_a_case_insensitive_database_match_when_the_username_is_not_an_ordinal_match()
    {
        await using var connection = CreateOpenConnection();
        await CreateCaseInsensitiveUsersTableAsync(connection);
        await using var setupContext = CreateContext(connection);
        setupContext.Users.Add(CreateUser("Alice"));
        await setupContext.SaveChangesAsync();

        var user = await new UserRepository(() => CreateContext(connection))
            .FindByUsernameAsync("alice", CancellationToken.None);

        Assert.Null(user);
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

    private static AuthDbContext CreateMySqlModelContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseSqlite("Data Source=:memory:")
            .ReplaceService<IDatabaseProvider, MySqlModelDatabaseProvider>()
            .Options;
        return new AuthDbContext(options);
    }

    private static void AssertColumn(
        IReadOnlyEntityType entity,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName,
        string columnType)
    {
        var property = Assert.IsAssignableFrom<IReadOnlyProperty>(entity.FindProperty(propertyName));
        Assert.Equal(columnName, property.GetColumnName(table));
        Assert.Equal(columnType, property.GetColumnType());
        Assert.False(property.IsNullable);
    }

    private static async Task CreateCaseInsensitiveUsersTableAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE jwt_users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT COLLATE NOCASE NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                is_enabled INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );";
        await command.ExecuteNonQueryAsync();
    }

    private sealed class MySqlModelDatabaseProvider : IDatabaseProvider
    {
        public string Name => "Pomelo.EntityFrameworkCore.MySql";

        public string Version => "synthetic-model-only-provider";

        public bool IsConfigured(IDbContextOptions options) => true;
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
