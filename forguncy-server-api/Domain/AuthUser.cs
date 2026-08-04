using System.ComponentModel.DataAnnotations.Schema;

namespace ForguncyServerApi.Domain;

public sealed class AuthUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int IsOpen { get; set; }

    // Task 3 removes the legacy bootstrap initializer which still assigns this value.
    [NotMapped]
    public bool IsEnabled { get; set; }

    // Task 3 removes the legacy bootstrap initializer which still assigns these values.
    // They are not part of the read-only c_userinfo entity mapping.
    [NotMapped]
    public DateTime CreatedAtUtc { get; set; }

    [NotMapped]
    public DateTime UpdatedAtUtc { get; set; }
}
