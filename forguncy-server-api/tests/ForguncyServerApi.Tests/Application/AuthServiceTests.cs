using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ForguncyServerApi.Application;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ForguncyServerApi.Tests.Application;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_returns_a_complete_token_pair_for_an_open_credit_code_with_a_valid_password()
    {
        var user = CreateUser(isEnabled: true);
        var service = TestService(
            users: new TrackingUsers(user),
            passwords: new StubPasswords(true, "synthetic-submitted-password"),
            tokens: TestJwtTokenService(accessLifetimeMinutes: 60, refreshLifetimeMinutes: 120),
            tokenLifetime: TimeSpan.FromMinutes(60),
            refreshTokenLifetime: TimeSpan.FromMinutes(120));

        var result = await service.LoginAsync(
            new LoginRequest("91330200SYNTHETIC", "synthetic-submitted-password"),
            CancellationToken.None);

        Assert.Equal(LoginStatus.Success, result.Status);
        Assert.NotNull(result.Tokens);
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens.RefreshToken));
        Assert.Equal(3600, result.Tokens.ExpiresInSeconds);
        Assert.Equal(7200, result.Tokens.RefreshExpiresInSeconds);
        Assert.Equal(3, result.User?.Id);
        Assert.Equal("91330200SYNTHETIC", result.User?.Username);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task RefreshAsync_rejects_blank_refresh_tokens(string refreshToken)
    {
        var result = await TestService().RefreshAsync(refreshToken, CancellationToken.None);

        Assert.Equal(RefreshStatus.InvalidRequest, result.Status);
        Assert.Null(result.Tokens);
    }

    [Theory]
    [MemberData(nameof(InvalidRefreshTokens))]
    public async Task RefreshAsync_maps_invalid_refresh_tokens_to_invalid_token(string scenario, string refreshToken)
    {
        var users = new TrackingUsers(CreateUser(isEnabled: true));
        var result = await TestService(users: users).RefreshAsync(refreshToken, CancellationToken.None);

        Assert.True(
            result.Status == RefreshStatus.InvalidToken,
            $"Scenario '{scenario}' should map to {RefreshStatus.InvalidToken}.");
        Assert.Null(result.Tokens);
        Assert.Equal(0, users.FindByUsernameCallCount);
    }

    [Fact]
    public async Task RefreshAsync_returns_a_complete_token_pair_without_querying_the_user_repository()
    {
        var users = new TrackingUsers(CreateUser(isEnabled: true));
        var tokens = TestJwtTokenService(accessLifetimeMinutes: 15, refreshLifetimeMinutes: 120);
        var service = TestService(
            users: users,
            tokens: tokens,
            tokenLifetime: TimeSpan.FromMinutes(15),
            refreshTokenLifetime: TimeSpan.FromMinutes(120));
        var refreshToken = tokens.CreateRefreshToken(new AuthUser
        {
            Id = 3,
            Username = "91330200SYNTHETIC",
            IsOpen = 1
        });

        var result = await service.RefreshAsync(refreshToken, CancellationToken.None);

        Assert.Equal(RefreshStatus.Success, result.Status);
        Assert.NotNull(result.Tokens);
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens.RefreshToken));
        Assert.Equal(900, result.Tokens.ExpiresInSeconds);
        Assert.Equal(7200, result.Tokens.RefreshExpiresInSeconds);
        Assert.Equal(0, users.FindByUsernameCallCount);
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
        var service = TestService(
            users: new TrackingUsers(null),
            passwords,
            tokenLifetime: TimeSpan.FromMinutes(60),
            refreshTokenLifetime: TimeSpan.FromMinutes(120));

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
        var service = TestService(
            users: new TrackingUsers(user),
            passwords,
            tokenLifetime: TimeSpan.FromMinutes(60),
            refreshTokenLifetime: TimeSpan.FromMinutes(120));

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
        var service = TestService(
            users: new TrackingUsers(user),
            passwords,
            tokenLifetime: TimeSpan.FromMinutes(60),
            refreshTokenLifetime: TimeSpan.FromMinutes(120));

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
        Assert.False(string.IsNullOrWhiteSpace(result.Tokens?.AccessToken));
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
            new TrackingUsers(null),
            new StubPasswords(false),
            TestJwtTokenService(),
            TimeSpan.FromSeconds(seconds),
            TimeSpan.FromMinutes(120)));
    }

    [Fact]
    public void Constructor_rejects_a_token_lifetime_that_exceeds_int_seconds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthService(
            new TrackingUsers(null),
            new StubPasswords(false),
            TestJwtTokenService(),
            TimeSpan.FromSeconds((double)int.MaxValue + 1),
            TimeSpan.FromMinutes(120)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_rejects_a_non_positive_refresh_token_lifetime(int seconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthService(
            new TrackingUsers(null),
            new StubPasswords(false),
            TestJwtTokenService(),
            TimeSpan.FromMinutes(60),
            TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Constructor_rejects_a_refresh_token_lifetime_that_exceeds_int_seconds()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AuthService(
            new TrackingUsers(null),
            new StubPasswords(false),
            TestJwtTokenService(),
            TimeSpan.FromMinutes(60),
            TimeSpan.FromSeconds((double)int.MaxValue + 1)));
    }

    public static IEnumerable<object[]> InvalidRefreshTokens()
    {
        var validUser = new AuthUser { Id = 3, Username = "91330200SYNTHETIC", IsOpen = 1 };
        var now = DateTime.UtcNow;

        yield return new object[]
        {
            "signed-with-wrong-key",
            CreateSignedToken(
                TestOptions(signingKey: "second-signing-key-that-is-at-least-32-chars"),
                now.AddMinutes(-1),
                now.AddMinutes(10))
        };
        yield return new object[]
        {
            "wrong-issuer",
            CreateSignedToken(TestOptions(), now.AddMinutes(-1), now.AddMinutes(10), issuer: "another-issuer")
        };
        yield return new object[]
        {
            "expired",
            CreateSignedToken(TestOptions(), now.AddMinutes(-10), now.AddMinutes(-5))
        };
        yield return new object[]
        {
            "malformed",
            "not-a-jwt"
        };
        yield return new object[]
        {
            "missing-sub",
            CreateSignedToken(TestOptions(), now.AddMinutes(-1), now.AddMinutes(10), sub: null)
        };
        yield return new object[]
        {
            "missing-name",
            CreateSignedToken(TestOptions(), now.AddMinutes(-1), now.AddMinutes(10), name: null)
        };
        yield return new object[]
        {
            "non-positive-sub",
            CreateSignedToken(TestOptions(), now.AddMinutes(-1), now.AddMinutes(10), sub: "0")
        };
        yield return new object[]
        {
            "wrong-type",
            CreateSignedToken(TestOptions(), now.AddMinutes(-1), now.AddMinutes(10), tokenUse: "access")
        };
    }

    private static AuthService TestService(
        IUserRepository? users = null,
        IPasswordHasher? passwords = null,
        IJwtTokenService? tokens = null,
        TimeSpan? tokenLifetime = null,
        TimeSpan? refreshTokenLifetime = null) => new(
        users ?? new TrackingUsers(CreateUser(isEnabled: true)),
        passwords ?? new StubPasswords(true, "synthetic-submitted-password"),
        tokens ?? TestJwtTokenService(),
        tokenLifetime ?? TimeSpan.FromMinutes(60),
        refreshTokenLifetime ?? TimeSpan.FromMinutes(120));

    private static AuthUser CreateUser(bool isEnabled, string passwordHash = "synthetic-password") => new()
    {
        Id = 3,
        Username = "91330200SYNTHETIC",
        PasswordHash = passwordHash,
        IsOpen = isEnabled ? 1 : 0
    };

    private static JwtTokenService TestJwtTokenService(
        string signingKey = "test-signing-key-that-is-at-least-32-chars",
        int accessLifetimeMinutes = 60,
        int refreshLifetimeMinutes = 120) =>
        new(TestOptions(signingKey, accessLifetimeMinutes, refreshLifetimeMinutes));

    private static AuthOptions TestOptions(
        string signingKey = "test-signing-key-that-is-at-least-32-chars",
        int accessLifetimeMinutes = 60,
        int refreshLifetimeMinutes = 120) =>
        AuthOptions.From(new Dictionary<string, string?>
        {
            ["FGC_JWT_SIGNING_KEY"] = signingKey,
            ["FGC_JWT_ISSUER"] = "synthetic-issuer",
            ["FGC_JWT_EXPIRES_MINUTES"] = accessLifetimeMinutes.ToString(CultureInfo.InvariantCulture),
            ["FGC_JWT_REFRESH_EXPIRES_MINUTES"] = refreshLifetimeMinutes.ToString(CultureInfo.InvariantCulture)
        });

    private static string CreateSignedToken(
        AuthOptions options,
        DateTime notBefore,
        DateTime expires,
        string? issuer = null,
        string? sub = "3",
        string? name = "91330200SYNTHETIC",
        string tokenUse = "refresh")
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(
                JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(notBefore).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64),
            new("token_use", tokenUse)
        };

        if (sub is not null)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, sub));
        }

        if (name is not null)
        {
            claims.Add(new Claim("name", name));
        }

        var token = new JwtSecurityToken(
            issuer: issuer ?? options.JwtIssuer,
            claims: claims,
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.JwtSigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TrackingUsers : IUserRepository
    {
        private readonly AuthUser? user;

        public TrackingUsers(AuthUser? user)
        {
            this.user = user;
        }

        public int FindByUsernameCallCount { get; private set; }

        public Task<AuthUser?> FindByUsernameAsync(string creditCode, CancellationToken cancellationToken)
        {
            FindByUsernameCallCount++;
            return Task.FromResult(user is not null && user.Username == creditCode ? user : null);
        }
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
}
