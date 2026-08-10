using System.Security.Cryptography;
using System.Text;

namespace ForguncyServerApi.Security;

public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        if (password is null)
        {
            throw new ArgumentNullException(nameof(password));
        }

        using var md5 = MD5.Create();
        var digest = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
        return BitConverter.ToString(digest).Replace("-", string.Empty).ToLowerInvariant().Substring(8, 16);
    }

    public bool Verify(string password, string encodedHash)
    {
        if (password is null || !IsLowercaseHex16(encodedHash))
        {
            return false;
        }

        var expected = Hash(password);
        var difference = 0;
        for (var index = 0; index < expected.Length; index++)
        {
            difference |= expected[index] ^ encodedHash[index];
        }

        return difference == 0;
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
