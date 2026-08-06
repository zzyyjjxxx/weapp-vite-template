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
    AuthUser? User);

public sealed record RefreshResult(
    RefreshStatus Status,
    TokenPair? Tokens);
