using ForguncyServerApi.Domain;
using ForguncyServerApi.Infrastructure;
using ForguncyServerApi.Security;

namespace ForguncyServerApi.Application;

public sealed class AuthService
{
    private const string DummyPasswordHash = "0000000000000000";

    private readonly IUserRepository users;
    private readonly IPasswordHasher passwords;
    private readonly IJwtTokenService tokens;
    private readonly TimeSpan tokenLifetime;

    public AuthService(
        IUserRepository users,
        IPasswordHasher passwords,
        IJwtTokenService tokens,
        TimeSpan tokenLifetime)
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

        this.tokenLifetime = tokenLifetime;
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
            tokens.CreateToken(user),
            new AuthUser { Id = user.Id, Username = user.Username },
            (int)tokenLifetime.TotalSeconds);
    }

    private static LoginResult InvalidRequest() => new(LoginStatus.InvalidRequest, null, null, 0);

    private static LoginResult InvalidCredentials() => new(LoginStatus.InvalidCredentials, null, null, 0);
}
