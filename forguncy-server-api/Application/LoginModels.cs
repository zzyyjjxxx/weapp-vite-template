using ForguncyServerApi.Domain;

namespace ForguncyServerApi.Application;

public sealed record LoginRequest(string Username, string Password);

public enum LoginStatus
{
    Success,
    InvalidRequest,
    InvalidCredentials
}

public sealed record LoginResult(
    LoginStatus Status,
    string? AccessToken,
    AuthUser? User,
    int ExpiresInSeconds);
