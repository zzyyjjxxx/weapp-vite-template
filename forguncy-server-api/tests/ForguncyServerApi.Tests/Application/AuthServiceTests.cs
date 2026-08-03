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
    public async Task LoginAsync_unifies_unknown_wrong_and_disabled_users_as_invalid_credentials()
    {
        var results = await Task.WhenAll(
            TestServiceWithMissingUser().LoginAsync(new LoginRequest("demo", "demo123"), CancellationToken.None),
            TestService().LoginAsync(new LoginRequest("demo", "wrong"), CancellationToken.None),
            TestServiceWithDisabledUser().LoginAsync(new LoginRequest("demo", "demo123"), CancellationToken.None));

        Assert.All(results, result =>
        {
            Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
            Assert.Null(result.AccessToken);
        });
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

    private static AuthService TestServiceWithDisabledUser() => new(
        new StubUsers(new AuthUser { Id = 3, Username = "demo", IsEnabled = false }),
        new StubPasswords(true, "demo123"),
        new StubTokens("signed-token"),
        TimeSpan.FromMinutes(60));

    private static AuthService TestServiceWithMissingUser() => new(
        new StubUsers(null),
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

        public string Hash(string password) => "synthetic-hash";

        public bool Verify(string password, string encodedHash) => valid && password == expectedPassword;
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
