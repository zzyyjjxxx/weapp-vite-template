using System.Security.Claims;
using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;
using Microsoft.IdentityModel.Tokens;

namespace ForguncyServerApi.Application;

public sealed class AuthService
{
    private const string DummyPasswordHash = "0000000000000000";

    private readonly IUserRepository users;
    private readonly IPasswordHasher passwords;
    private readonly IJwtTokenService tokens;
    private readonly TimeSpan tokenLifetime;
    private readonly TimeSpan refreshTokenLifetime;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwords,
        IJwtTokenService tokens,
        TimeSpan tokenLifetime,
        TimeSpan refreshTokenLifetime)
    {
        this.users = users ?? throw new ArgumentNullException(nameof(users));
        this.passwords = passwords ?? throw new ArgumentNullException(nameof(passwords));
        this.tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
        if (tokenLifetime <= TimeSpan.Zero || tokenLifetime.TotalSeconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tokenLifetime),
                "Token lifetime must be positive and fit in Int32 seconds.");
        }

        if (refreshTokenLifetime <= TimeSpan.Zero || refreshTokenLifetime.TotalSeconds > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException(
                nameof(refreshTokenLifetime),
                "Refresh token lifetime must be positive and fit in Int32 seconds.");
        }

        this.tokenLifetime = tokenLifetime;
        this.refreshTokenLifetime = refreshTokenLifetime;
    }

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var creditCode = request.Username?.Trim();
        if (string.IsNullOrEmpty(creditCode) || string.IsNullOrEmpty(request.Password))
        {
            return InvalidRequest();
        }

        var user = await users.FindByUsernameAsync(creditCode!, cancellationToken);
        var passwordIsValid = passwords.Verify(request.Password, user?.PasswordHash ?? DummyPasswordHash);
        if (user is null || user.IsOpen != 1 || !passwordIsValid)
        {
            return InvalidCredentials();
        }

        return new LoginResult(
            LoginStatus.Success,
            CreateTokenPair(user),
            new AuthUser { Id = user.Id, Username = user.Username });
    }

    public Task<RefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Task.FromResult(InvalidRefreshRequest());
        }

        try
        {
            var principal = tokens.ValidateRefreshToken(refreshToken);
            if (!TryCreateRefreshUser(principal, out var user))
            {
                return Task.FromResult(InvalidRefreshToken());
            }

            return Task.FromResult(new RefreshResult(RefreshStatus.Success, CreateTokenPair(user!)));
        }
        catch (SecurityTokenException)
        {
            return Task.FromResult(InvalidRefreshToken());
        }
    }

    private static LoginResult InvalidRequest() => new(LoginStatus.InvalidRequest, null, null);

    private static LoginResult InvalidCredentials() => new(LoginStatus.InvalidCredentials, null, null);

    private static RefreshResult InvalidRefreshRequest() => new(RefreshStatus.InvalidRequest, null);

    private static RefreshResult InvalidRefreshToken() => new(RefreshStatus.InvalidToken, null);

    private bool TryCreateRefreshUser(ClaimsPrincipal principal, out AuthUser? user)
    {
        user = null;
        var subject = principal.FindFirst("sub")?.Value;
        var username = principal.FindFirst("name")?.Value;
        if (!int.TryParse(subject, out var id) || id <= 0 || string.IsNullOrWhiteSpace(username))
        {
            return false;
        }

        user = new AuthUser
        {
            Id = id,
            Username = username!
        };
        return true;
    }

    private TokenPair CreateTokenPair(AuthUser user) =>
        new(
            tokens.CreateToken(user),
            tokens.CreateRefreshToken(user),
            (int)tokenLifetime.TotalSeconds,
            (int)refreshTokenLifetime.TotalSeconds);
}
