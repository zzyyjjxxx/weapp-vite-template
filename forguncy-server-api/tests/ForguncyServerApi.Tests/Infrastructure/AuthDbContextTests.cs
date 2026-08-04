using System.Security.Claims;
using ForguncyServerApi.Application;
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
    public void AuthUser_exposes_only_the_c_userinfo_fields()
    {
        Assert.Equal(
            new[] { "Id", "IsOpen", "PasswordHash", "Username" },
            typeof(AuthUser).GetProperties().Select(property => property.Name).OrderBy(name => name));
    }

    [Fact]
    public void MySql_model_matches_the_c_userinfo_schema_contract()
    {
        using var context = CreateMySqlModelContext();

        var user = Assert.IsAssignableFrom<IReadOnlyEntityType>(
            context.Model.FindEntityType(typeof(AuthUser)));
        Assert.Equal("c_userinfo", user.GetTableName());
        Assert.Equal("utf8mb4", user.FindAnnotation("MySql:CharSet")?.Value);
        Assert.Equal("utf8mb4_unicode_ci", user.FindAnnotation("Relational:Collation")?.Value);

        var table = StoreObjectIdentifier.Table("c_userinfo", schema: null);
        AssertColumn(user, table, nameof(AuthUser.Id), "id", "int");
        AssertColumn(user, table, nameof(AuthUser.Username), "creditCode", "varchar(255)");
        AssertColumn(user, table, nameof(AuthUser.PasswordHash), "password", "varchar(255)");
        AssertColumn(user, table, nameof(AuthUser.IsOpen), "isopen", "int");

        Assert.Empty(user.GetIndexes());

        var id = Assert.IsAssignableFrom<IReadOnlyProperty>(user.FindProperty(nameof(AuthUser.Id)));
        Assert.Equal(ValueGenerated.Never, id.ValueGenerated);

    }

    [Fact]
    public async Task FindByUsernameAsync_looks_up_the_exact_credit_code_and_preserves_isopen()
    {
        await using var connection = CreateOpenConnection();
        await CreateCUserinfoTableAsync(connection);
        await InsertUserAsync(connection, 17, "91330200SYNTHETIC", "synthetic-password", 0);

        var repository = new UserRepository(() => CreateContext(connection));

        var user = await repository.FindByUsernameAsync("91330200SYNTHETIC", CancellationToken.None);

        Assert.NotNull(user);
        Assert.Equal(17, user.Id);
        Assert.Equal("91330200SYNTHETIC", user.Username);
        Assert.Equal("synthetic-password", user.PasswordHash);
        Assert.Equal(0, user.IsOpen);
        Assert.Null(await repository.FindByUsernameAsync("91330200synthetic", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_rejects_a_mapped_isopen_value_other_than_one()
    {
        await using var connection = CreateOpenConnection();
        await CreateCUserinfoTableAsync(connection);
        var creditCode = nameof(AuthDbContextTests);
        await InsertUserAsync(connection, 19, creditCode, nameof(IPasswordHasher), 2);

        var service = new AuthService(
            new UserRepository(() => CreateContext(connection)),
            new AcceptingPasswords(),
            new StubTokens(),
            TimeSpan.FromMinutes(60));

        var mappedUser = await new UserRepository(() => CreateContext(connection))
            .FindByUsernameAsync(creditCode, CancellationToken.None);
        Assert.Equal(2, mappedUser?.IsOpen);

        var result = await service.LoginAsync(
            new LoginRequest(creditCode, nameof(LoginRequest)),
            CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.AccessToken);
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

    private static async Task CreateCUserinfoTableAsync(SqliteConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE c_userinfo (
                id INTEGER NOT NULL PRIMARY KEY,
                creditCode TEXT NOT NULL,
                password TEXT NOT NULL,
                isopen INTEGER NOT NULL
            );";
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertUserAsync(
        SqliteConnection connection,
        int id,
        string creditCode,
        string password,
        int isOpen)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO c_userinfo (id, creditCode, password, isopen)
            VALUES ($id, $creditCode, $password, $isopen);";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$creditCode", creditCode);
        command.Parameters.AddWithValue("$password", password);
        command.Parameters.AddWithValue("$isopen", isOpen);
        await command.ExecuteNonQueryAsync();
    }

    private sealed class MySqlModelDatabaseProvider : IDatabaseProvider
    {
        public string Name => "Pomelo.EntityFrameworkCore.MySql";

        public string Version => "synthetic-model-only-provider";

        public bool IsConfigured(IDbContextOptions options) => true;
    }

    private sealed class AcceptingPasswords : IPasswordHasher
    {
        public string Hash(string password) => string.Empty;

        public bool Verify(string password, string encodedHash) => true;
    }

    private sealed class StubTokens : IJwtTokenService
    {
        public string CreateToken(AuthUser user) => string.Empty;

        public ClaimsPrincipal ValidateToken(string token) => new();
    }
}
