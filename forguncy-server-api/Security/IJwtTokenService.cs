using System.Security.Claims;
using ForguncyServerApi.Domain;

namespace ForguncyServerApi.Security;

public interface IJwtTokenService
{
    string CreateToken(AuthUser user);

    ClaimsPrincipal ValidateToken(string token);
}
