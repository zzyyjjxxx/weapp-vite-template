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

    [Fact]
    public void Hash_uses_the_required_algorithm_parameters_and_encoded_sizes()
    {
        var encoded = new PasswordHasher().Hash("synthetic password");
        var parts = encoded.Split('$');

        Assert.Equal(4, parts.Length);
        Assert.Equal("PBKDF2-SHA256", parts[0]);
        Assert.Equal("100000", parts[1]);
        Assert.Equal(16, Convert.FromBase64String(parts[2]).Length);
        Assert.Equal(32, Convert.FromBase64String(parts[3]).Length);
    }

    [Fact]
    public void Hash_uses_a_different_salt_for_repeated_hashes()
    {
        var hasher = new PasswordHasher();
        var firstSalt = hasher.Hash("synthetic password").Split('$')[2];
        var secondSalt = hasher.Hash("synthetic password").Split('$')[2];

        Assert.NotEqual(firstSalt, secondSalt);
    }

    [Fact]
    public void Verify_rejects_unsupported_algorithms_and_iteration_values()
    {
        var parts = new PasswordHasher().Hash("synthetic password").Split('$');

        parts[0] = "PBKDF2-SHA512";
        Assert.False(new PasswordHasher().Verify("synthetic password", string.Join('$', parts)));

        parts = new PasswordHasher().Hash("synthetic password").Split('$');
        parts[1] = "99999";
        Assert.False(new PasswordHasher().Verify("synthetic password", string.Join('$', parts)));
    }

    [Fact]
    public void Verify_rejects_null_inputs()
    {
        var encoded = new PasswordHasher().Hash("synthetic password");
        var hasher = new PasswordHasher();

        Assert.False(hasher.Verify(null!, encoded));
        Assert.False(hasher.Verify("synthetic password", null!));
    }

    [Fact]
    public void Verify_rejects_invalid_base64_segments()
    {
        var parts = new PasswordHasher().Hash("synthetic password").Split('$');
        var hasher = new PasswordHasher();

        parts[2] = "not-base64";
        Assert.False(hasher.Verify("synthetic password", string.Join('$', parts)));

        parts = new PasswordHasher().Hash("synthetic password").Split('$');
        parts[3] = "not-base64";
        Assert.False(hasher.Verify("synthetic password", string.Join('$', parts)));
    }

    [Fact]
    public void Verify_rejects_invalid_decoded_salt_and_key_lengths()
    {
        var parts = new PasswordHasher().Hash("synthetic password").Split('$');
        var hasher = new PasswordHasher();

        parts[2] = Convert.ToBase64String(new byte[15]);
        Assert.False(hasher.Verify("synthetic password", string.Join('$', parts)));

        parts = new PasswordHasher().Hash("synthetic password").Split('$');
        parts[3] = Convert.ToBase64String(new byte[31]);
        Assert.False(hasher.Verify("synthetic password", string.Join('$', parts)));
    }
}
