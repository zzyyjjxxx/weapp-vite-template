using System.Security.Claims;
using ForguncyServerApi.Application;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using Xunit;

namespace ForguncyServerApi.Tests.Application;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_returns_a_token_for_an_enabled_user_with_a_valid_password()
    {
        var user = new AuthUser { Id = 3, Username = "demo", IsEnabled = true };
        var service = new AuthService(
            new StubUsers(user),
            new StubPasswords(true, "demo123"),
            new StubTokens("signed-token"),
            TimeSpan.FromMinutes(60));

        var result = await service.LoginAsync(new LoginRequest("demo", "demo123"), CancellationToken.None);

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal("signed-token", result.AccessToken);
        Assert.Equal(3600, result.ExpiresInSeconds);
        Assert.Equal(3, result.User?.Id);
        Assert.Equal("demo", result.User?.Username);
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("demo", "")]
    public async Task LoginAsync_rejects_missing_credentials(string username, string password)
    {
        var result = await TestService().LoginAsync(new LoginRequest(username, password), CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidRequest, result.Status);
    }

    [Fact]
    public async Task LoginAsync_verifies_once_and_rejects_a_missing_user()
    {
        var passwords = new StubPasswords(true, "submitted-password");
        var service = new AuthService(
            new StubUsers(null),
            passwords,
            new StubTokens("signed-token"),
            TimeSpan.FromMinutes(60));

        var result = await service.LoginAsync(
            new LoginRequest("missing", "submitted-password"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.AccessToken);
        Assert.Equal(1, passwords.VerifyCallCount);
        Assert.True(new PasswordHasher().Verify(string.Empty, passwords.LastEncodedHash!));
    }

    [Fact]
    public async Task LoginAsync_verifies_once_and_rejects_a_disabled_user()
    {
        var user = new AuthUser
        {
            Id = 3,
            Username = "demo",
            PasswordHash = "stored-disabled-hash",
            IsEnabled = false
        };
        var passwords = new StubPasswords(true, "submitted-password");
        var service = new AuthService(
            new StubUsers(user),
            passwords,
            new StubTokens("signed-token"),
            TimeSpan.FromMinutes(60));

        var result = await service.LoginAsync(
            new LoginRequest("demo", "submitted-password"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.AccessToken);
        Assert.Equal(1, passwords.VerifyCallCount);
        Assert.Equal(user.PasswordHash, passwords.LastEncodedHash);
    }

    [Fact]
    public async Task LoginAsync_verifies_once_and_rejects_an_enabled_user_with_a_wrong_password()
    {
        var user = new AuthUser
        {
            Id = 3,
            Username = "demo",
            PasswordHash = "stored-enabled-hash",
            IsEnabled = true
        };
        var passwords = new StubPasswords(false);
        var service = new AuthService(
            new StubUsers(user),
            passwords,
            new StubTokens("signed-token"),
            TimeSpan.FromMinutes(60));

        var result = await service.LoginAsync(
            new LoginRequest("demo", "wrong"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.AccessToken);
        Assert.Equal(1, passwords.VerifyCallCount);
        Assert.Equal(user.PasswordHash, passwords.LastEncodedHash);
    }

    [Fact]
    public async Task LoginAsync_trims_only_the_username_before_authentication()
    {
        var result = await TestService().LoginAsync(
            new LoginRequest(" demo ", "demo123"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal("signed-token", result.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_does_not_trim_a_trailing_space_from_the_password()
    {
        var result = await TestService().LoginAsync(
            new LoginRequest("demo", "demo123 "),
            CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_rejects_a_non_positive_token_lifetime(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthService(
            new StubUsers(null),
            new StubPasswords(false),
            new StubTokens("signed-token"),
            TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Constructor_rejects_a_token_lifetime_that_exceeds_int_seconds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthService(
            new StubUsers(null),
            new StubPasswords(false),
            new StubTokens("signed-token"),
            TimeSpan.FromSeconds((double)int.MaxValue + 1)));
    }

    private static AuthService TestService() => new(
        new StubUsers(new AuthUser { Id = 3, Username = "demo", IsEnabled = true }),
        new StubPasswords(true, "demo123"),
        new StubTokens("signed-token"),
        TimeSpan.FromMinutes(60));

    private sealed class StubUsers : IUserRepository
    {
        private readonly AuthUser? user;

        public StubUsers(AuthUser? user)
        {
            this.user = user;
        }

        public Task<AuthUser?> FindByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(user is not null && user.Username == username ? user : null);

        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult(user is not null && user.Username == username);

        public Task AddAsync(AuthUser user, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class StubPasswords : IPasswordHasher
    {
        private readonly bool valid;
        private readonly string expectedPassword;

        public StubPasswords(bool valid, string expectedPassword = "")
        {
            this.valid = valid;
            this.expectedPassword = expectedPassword;
        }

        public int VerifyCallCount { get; private set; }

        public string? LastEncodedHash { get; private set; }

        public string Hash(string password) => "synthetic-hash";

        public bool Verify(string password, string encodedHash)
        {
            VerifyCallCount++;
            LastEncodedHash = encodedHash;
            return valid && password == expectedPassword;
        }
    }

    private sealed class StubTokens : IJwtTokenService
    {
        private readonly string token;

        public StubTokens(string token)
        {
            this.token = token;
        }

        public string CreateToken(AuthUser user) => token;

        public ClaimsPrincipal ValidateToken(string token) => new();
    }
}
