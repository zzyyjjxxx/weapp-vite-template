using ForguncyServerApi.Security;
using Xunit;

namespace ForguncyServerApi.Tests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_returns_the_lowercase_middle_md5_value()
    {
        var encoded = new PasswordHasher().Hash("password");

        Assert.Equal("5aa765d61d8327de", encoded);
        Assert.Equal(16, encoded.Length);
        Assert.Equal(encoded.ToLowerInvariant(), encoded);
    }

    [Fact]
    public void Verify_accepts_the_original_password_and_rejects_another_password()
    {
        var hasher = new PasswordHasher();
        const string encoded = "5aa765d61d8327de";

        Assert.True(hasher.Verify("password", encoded));
        Assert.False(hasher.Verify("wrong password", encoded));
    }

    [Fact]
    public void Hash_returns_the_same_value_for_repeated_hashes()
    {
        var hasher = new PasswordHasher();

        Assert.Equal(hasher.Hash("password"), hasher.Hash("password"));
    }

    [Fact]
    public void Verify_rejects_malformed_length_and_characters()
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify("password", "5aa765d61d8327d"));
        Assert.False(hasher.Verify("password", "5aa765d61d8327de0"));
        Assert.False(hasher.Verify("password", "5aa765d61d8327dD"));
        Assert.False(hasher.Verify("password", "5aa765d61d8327d-"));
    }

    [Fact]
    public void Verify_rejects_null_inputs()
    {
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify(null!, "5aa765d61d8327de"));
        Assert.False(hasher.Verify("password", null!));
    }
}
