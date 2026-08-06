using ForguncyServerApi.Application;
using ForguncyServerApi.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;

namespace ForguncyServerApi.Api;

public static class AccessTokenReader
{
    public static Task<EnterpriseIdentity> ReadRequiredIdentity(
        HttpRequest request,
        IJwtTokenService tokens,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (tokens is null)
        {
            throw new ArgumentNullException(nameof(tokens));
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var token = ReadBearerToken(request.Headers["Authorization"]);
            var principal = tokens.ValidateAccessToken(token);
            var userId = ReadRequiredPositiveInt(principal.FindFirst("sub")?.Value);
            var creditCode = ReadRequiredCreditCode(principal.FindFirst("name")?.Value);
            return Task.FromResult(new EnterpriseIdentity(userId, creditCode));
        }
        catch (SecurityTokenException)
        {
            throw CreateFormatException();
        }
        catch (ArgumentException)
        {
            throw CreateFormatException();
        }
    }

    private static string ReadBearerToken(StringValues authorization)
    {
        if (authorization.Count != 1)
        {
            throw CreateFormatException();
        }

        var header = authorization[0];
        const string prefix = "Bearer ";
        if (string.IsNullOrEmpty(header) || !header.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw CreateFormatException();
        }

        var token = header.Substring(prefix.Length);
        if (string.IsNullOrWhiteSpace(token) || token.Any(char.IsWhiteSpace))
        {
            throw CreateFormatException();
        }

        return token;
    }

    private static int ReadRequiredPositiveInt(string? value)
    {
        return int.TryParse(value, out var parsed) && parsed > 0
            ? parsed
            : throw CreateFormatException();
    }

    private static string ReadRequiredCreditCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw CreateFormatException();
        }

        return value!;
    }

    private static Exception CreateFormatException() => new AccessTokenFormatException();

    private sealed class AccessTokenFormatException : Exception
    {
        public AccessTokenFormatException()
            : base("The access token format is invalid.")
        {
        }
    }
}
