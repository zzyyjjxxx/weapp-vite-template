using System.Security.Cryptography;
using System.Text;

namespace ForguncyServerApi.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var digest = MD5.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(digest).ToLowerInvariant().Substring(8, 16);
    }

    public bool Verify(string password, string encodedHash)
    {
        if (password is null || !IsLowercaseHex16(encodedHash))
        {
            return false;
        }

        var expected = Encoding.UTF8.GetBytes(Hash(password));
        var stored = Encoding.UTF8.GetBytes(encodedHash);
        return CryptographicOperations.FixedTimeEquals(expected, stored);
    }

    private static bool IsLowercaseHex16(string? value)
    {
        if (value is null || value.Length != 16)
        {
            return false;
        }

        foreach (var character in value)
        {
            if ((character < '0' || character > '9') && (character < 'a' || character > 'f'))
            {
                return false;
            }
        }

        return true;
    }
}
