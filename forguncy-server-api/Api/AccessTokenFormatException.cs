namespace ForguncyServerApi.Api;

internal sealed class AccessTokenFormatException : Exception
{
    public AccessTokenFormatException()
        : base("The access token format is invalid.")
    {
    }
}
