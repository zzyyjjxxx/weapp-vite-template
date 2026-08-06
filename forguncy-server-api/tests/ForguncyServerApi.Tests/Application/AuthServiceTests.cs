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
    public async Task LoginAsync_returns_the_existing_jwt_response_for_an_open_credit_code_with_a_valid_password()
    {
        var user = CreateUser(isEnabled: true);
        var service = new AuthService(
            new StubUsers(user),
            new StubPasswords(true, "synthetic-submitted-password"),
            new StubTokens("signed-token"),
            TimeSpan.FromMinutes(60));

        var result = await service.LoginAsync(
            new LoginRequest("91330200SYNTHETIC", "synthetic-submitted-password"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.NotNull(result.Tokens);
        Assert.Equal("signed-token", result.Tokens!.AccessToken);
        Assert.Equal(3600, result.Tokens.ExpiresInSeconds);
        Assert.Equal(3, result.User?.Id);
        Assert.Equal("91330200SYNTHETIC", result.User?.Username);
    }

    [Theory]
    [InlineData("", "synthetic-submitted-password")]
    [InlineData("91330200SYNTHETIC", "")]
    public async Task LoginAsync_rejects_missing_credentials(string username, string password)
    {
        var result = await TestService().LoginAsync(new LoginRequest(username, password), CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidRequest, result.Status);
    }

    [Fact]
    public async Task LoginAsync_verifies_once_and_rejects_a_missing_credit_code()
    {
        var passwords = new StubPasswords(true, "synthetic-submitted-password");
        var service = new AuthService(
            new StubUsers(null),
            passwords,
            new StubTokens("signed-token"),
            TimeSpan.FromMinutes(60));

        var result = await service.LoginAsync(
            new LoginRequest("91330200MISSING", "synthetic-submitted-password"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.Tokens);
        Assert.Equal(1, passwords.VerifyCallCount);
        Assert.Equal("0000000000000000", passwords.LastEncodedHash);
    }

    [Fact]
    public async Task LoginAsync_rejects_a_closed_credit_code_when_isopen_is_zero()
    {
        var user = CreateUser(isEnabled: false, passwordHash: "synthetic-closed-password");
        var passwords = new StubPasswords(true, "synthetic-submitted-password");
        var service = new AuthService(
            new StubUsers(user),
            passwords,
            new StubTokens("signed-token"),
            TimeSpan.FromMinutes(60));

        var result = await service.LoginAsync(
            new LoginRequest("91330200SYNTHETIC", "synthetic-submitted-password"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.Tokens);
        Assert.Equal(1, passwords.VerifyCallCount);
        Assert.Equal(user.PasswordHash, passwords.LastEncodedHash);
    }

    [Fact]
    public async Task LoginAsync_rejects_an_open_credit_code_with_a_wrong_password()
    {
        var user = CreateUser(isEnabled: true, passwordHash: "synthetic-open-password");
        var passwords = new StubPasswords(false);
        var service = new AuthService(
            new StubUsers(user),
            passwords,
            new StubTokens("signed-token"),
            TimeSpan.FromMinutes(60));

        var result = await service.LoginAsync(
            new LoginRequest("91330200SYNTHETIC", "synthetic-wrong-password"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.InvalidCredentials, result.Status);
        Assert.Null(result.Tokens);
        Assert.Equal(1, passwords.VerifyCallCount);
        Assert.Equal(user.PasswordHash, passwords.LastEncodedHash);
    }

    [Fact]
    public async Task LoginAsync_trims_only_the_username_before_credit_code_lookup()
    {
        var result = await TestService().LoginAsync(
            new LoginRequest(" 91330200SYNTHETIC ", "synthetic-submitted-password"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.Equal("signed-token", result.Tokens?.AccessToken);
    }

    [Fact]
    public async Task LoginAsync_does_not_trim_a_trailing_space_from_the_password()
    {
        var result = await TestService().LoginAsync(
            new LoginRequest("91330200SYNTHETIC", "synthetic-submitted-password "),
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
        new StubUsers(CreateUser(isEnabled: true)),
        new StubPasswords(true, "synthetic-submitted-password"),
        new StubTokens("signed-token"),
        TimeSpan.FromMinutes(60));

    private static AuthUser CreateUser(bool isEnabled, string passwordHash = "synthetic-password") => new()
    {
        Id = 3,
        Username = "91330200SYNTHETIC",
        PasswordHash = passwordHash,
        IsOpen = isEnabled ? 1 : 0
    };

    private sealed class StubUsers : IUserRepository
    {
        private readonly AuthUser? user;

        public StubUsers(AuthUser? user)
        {
            this.user = user;
        }

        public Task<AuthUser?> FindByUsernameAsync(string creditCode, CancellationToken cancellationToken) =>
            Task.FromResult(user is not null && user.Username == creditCode ? user : null);
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
