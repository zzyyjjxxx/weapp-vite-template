using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ForguncyServerApi.Configuration;
using ForguncyServerApi.Domain;
using Microsoft.IdentityModel.Tokens;

namespace ForguncyServerApi.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly AuthOptions options;
    private readonly SymmetricSecurityKey signingKey;
    private readonly JwtSecurityTokenHandler tokenHandler = new() { MapInboundClaims = false };

    public JwtTokenService(AuthOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.JwtSigningKey));
    }

    public string CreateToken(AuthUser user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var now = DateTime.UtcNow;
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim("name", user.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)
        };
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: options.JwtIssuer,
            claims: claims,
            notBefore: now,
            expires: now.Add(options.JwtLifetime),
            signingCredentials: credentials);

        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal ValidateToken(string token)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("A JWT is required.", nameof(token));
            }

            return tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                IssuerSigningKey = signingKey,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidateIssuer = true,
                ValidIssuer = options.JwtIssuer,
                ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 },
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,
                RequireExpirationTime = true,
                ValidateAudience = false
            }, out _);
        }
        catch (ArgumentException exception)
        {
            throw new SecurityTokenException("The JWT is malformed.", exception);
        }
    }
}
