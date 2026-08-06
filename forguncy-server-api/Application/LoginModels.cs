using ForguncyServerApi.Domain;

namespace ForguncyServerApi.Application;

public sealed record LoginRequest(string Username, string Password);

public enum LoginStatus
{
    Success,
    InvalidRequest,
    InvalidCredentials
}

public sealed record TokenPair(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    int RefreshExpiresInSeconds);

public enum RefreshStatus
{
    Success,
    InvalidRequest,
    InvalidToken
}

public sealed record LoginResult(
    LoginStatus Status,
    TokenPair? Tokens,
    AuthUser? User)
{
    public LoginResult(LoginStatus status, string? accessToken, AuthUser? user, int expiresInSeconds)
        : this(
            status,
            accessToken is null
                ? null
                : new TokenPair(accessToken, string.Empty, expiresInSeconds, 0),
            user)
    {
    }

    public string? AccessToken => Tokens?.AccessToken;

    public int ExpiresInSeconds => Tokens?.ExpiresInSeconds ?? 0;
}

public sealed record RefreshResult(
    RefreshStatus Status,
    TokenPair? Tokens);
