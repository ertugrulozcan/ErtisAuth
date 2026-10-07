using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

public class ApplicationSecretHelperTests
{
	#region Generate
	
	[Fact]
	public void GenerateSecret_ReturnsUrlSafeSecretWithoutBasicTokenSeparator()
	{
		var secret = ApplicationSecretHelper.GenerateSecret();
		
		// 32 random bytes, base64url without padding
		Assert.Equal(43, secret.Length);
		Assert.Matches("^[A-Za-z0-9_-]+$", secret);
		Assert.DoesNotContain(':', secret);
	}
	
	[Fact]
	public void GenerateSecret_ReturnsDifferentSecretEachTime()
	{
		var secrets = Enumerable.Range(0, 100).Select(_ => ApplicationSecretHelper.GenerateSecret()).ToHashSet();
		
		Assert.Equal(100, secrets.Count);
	}
	
	#endregion
	
	#region Hash & Verify
	
	[Fact]
	public void HashSecret_ReturnsLowercaseSha256Hex()
	{
		// SHA-256("abc")
		Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", ApplicationSecretHelper.HashSecret("abc"));
	}
	
	[Fact]
	public void VerifySecret_WithMatchingSecret_ReturnsTrue()
	{
		var secret = ApplicationSecretHelper.GenerateSecret();
		
		Assert.True(ApplicationSecretHelper.VerifySecret(secret, ApplicationSecretHelper.HashSecret(secret)));
	}
	
	[Theory]
	[InlineData("")]
	[InlineData("wrong-secret")]
	public void VerifySecret_WithDifferentSecret_ReturnsFalse(string secret)
	{
		var secretHash = ApplicationSecretHelper.HashSecret(ApplicationSecretHelper.GenerateSecret());
		
		Assert.False(ApplicationSecretHelper.VerifySecret(secret, secretHash));
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("not-a-hex-string")]
	[InlineData("abc")]
	public void VerifySecret_WithMissingOrMalformedHash_ReturnsFalse(string? secretHash)
	{
		Assert.False(ApplicationSecretHelper.VerifySecret("secret", secretHash));
	}
	
	#endregion
}
