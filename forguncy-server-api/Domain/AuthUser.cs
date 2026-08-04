namespace ForguncyServerApi.Domain;

public sealed class AuthUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int IsOpen { get; set; }
}
