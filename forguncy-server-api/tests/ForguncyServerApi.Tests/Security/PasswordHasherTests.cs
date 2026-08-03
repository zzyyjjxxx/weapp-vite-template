using ForguncyServerApi.Security;
using Xunit;

namespace ForguncyServerApi.Tests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_accepts_the_original_password_and_rejects_another_password()
    {
        var hasher = new PasswordHasher();
        var encoded = hasher.Hash("correct horse battery staple");

        Assert.True(hasher.Verify("correct horse battery staple", encoded));
        Assert.False(hasher.Verify("wrong password", encoded));
        Assert.NotEqual("correct horse battery staple", encoded);
    }

    [Fact]
    public void Verify_rejects_malformed_hashes()
    {
        Assert.False(new PasswordHasher().Verify("password", "not-a-pbkdf2-value"));
    }
}
