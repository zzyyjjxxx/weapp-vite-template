using System.Security.Claims;
using ForguncyServerApi.Domain;

namespace ForguncyServerApi.Security;

public interface IJwtTokenService
{
    string CreateToken(AuthUser user);

    string CreateRefreshToken(AuthUser user);

    ClaimsPrincipal ValidateAccessToken(string token);

    ClaimsPrincipal ValidateToken(string token);

    ClaimsPrincipal ValidateRefreshToken(string token);
}
