using System.Text;
using ErtisAuth.Core.Models.Cryptography;
using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// Tests for the modern (salted, PHC formatted) password hashing algorithms.
/// Known-answer vectors are computed independently: Argon2id with OpenSSL 3.3 (`openssl kdf ... ARGON2ID`),
/// PBKDF2 with Python hashlib.pbkdf2_hmac. Salt is "ErtisAuthSalt!16" (RXJ0aXNBdXRoU2FsdCExNg).
/// </summary>
public class PasswordHasherTests
{
	#region Constants
	
	private const string Password = "P@ssw0rd!";
	
	private const string UnicodePassword = "Şifre-Güçlü-ığüşöç";
	
	private const string Argon2idVector = "$argon2id$v=19$m=19456,t=2,p=1$RXJ0aXNBdXRoU2FsdCExNg$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc";
	
	private const string Argon2idUtf16Vector = "$argon2id$v=19$m=19456,t=2,p=1$RXJ0aXNBdXRoU2FsdCExNg$MOxFCjHbTBOqRsIabnNrER3Bt0Kuk2mGpS0ZIcDYoBE";
	
	private const string Pbkdf2Sha256Vector = "$pbkdf2-sha256$i=600000$RXJ0aXNBdXRoU2FsdCExNg$KHerChsPs4Ruu9BoHAgcQZwRsn9TwHuFh32Nm17nPTU";
	
	private const string Pbkdf2Sha512Vector = "$pbkdf2-sha512$i=210000$RXJ0aXNBdXRoU2FsdCExNg$EyVtj8iEUqCqsFGtUYNm5qVfvK4KkX8D02ZxVgm7S9il8HfNNqpjv++5W8FB+vXN7Xq5j0o1+CpMdklOaSJPIw";
	
	private const string Pbkdf2Sha256LowIterationVector = "$pbkdf2-sha256$i=1000$RXJ0aXNBdXRoU2FsdCExNg$YIszBJS6uOv/h0hEPUOqc0nrcQDGtIKyyuL9rR+o9Ho";
	
	private const string Sha256OfPassword = "0e44ce7308af2b3de5232e4616403ce7d49ba2aec83f79c196409556422a4927";
	
	#endregion
	
	#region Known Answer Vectors
	
	[Theory]
	[InlineData(Argon2idVector)]
	[InlineData(Pbkdf2Sha256Vector)]
	[InlineData(Pbkdf2Sha512Vector)]
	[InlineData(Pbkdf2Sha256LowIterationVector)]
	public void VerifyPassword_WithIndependentlyComputedVector_ReturnsTrue(string storedHash)
	{
		Assert.True(PasswordHasher.VerifyPassword(Password, storedHash, HashAlgorithms.ARGON2ID, Encoding.UTF8));
	}
	
	[Theory]
	[InlineData(Argon2idVector)]
	[InlineData(Pbkdf2Sha256Vector)]
	[InlineData(Pbkdf2Sha512Vector)]
	public void VerifyPassword_WithWrongPassword_ReturnsFalse(string storedHash)
	{
		Assert.False(PasswordHasher.VerifyPassword("wrong-password", storedHash, HashAlgorithms.ARGON2ID, Encoding.UTF8));
	}
	
	[Fact]
	public void VerifyPassword_WithModernAlgorithm_UsesGivenEncoding()
	{
		Assert.True(PasswordHasher.VerifyPassword(UnicodePassword, Argon2idUtf16Vector, HashAlgorithms.ARGON2ID, Encoding.Unicode));
		Assert.False(PasswordHasher.VerifyPassword(UnicodePassword, Argon2idUtf16Vector, HashAlgorithms.ARGON2ID, Encoding.UTF8));
	}
	
	#endregion
	
	#region Hash Format
	
	[Theory]
	[InlineData(HashAlgorithms.ARGON2ID, "$argon2id$v=19$m=19456,t=2,p=1$")]
	[InlineData(HashAlgorithms.PBKDF2_SHA256, "$pbkdf2-sha256$i=600000$")]
	[InlineData(HashAlgorithms.PBKDF2_SHA512, "$pbkdf2-sha512$i=210000$")]
	public void HashPassword_WithModernAlgorithm_ReturnsPhcStringWithOwaspParameters(HashAlgorithms algorithm, string expectedPrefix)
	{
		var hash = PasswordHasher.HashPassword(Password, algorithm, Encoding.UTF8);
		
		Assert.StartsWith(expectedPrefix, hash);
		Assert.DoesNotContain("=", hash[expectedPrefix.Length..]);
	}
	
	[Theory]
	[InlineData(HashAlgorithms.ARGON2ID)]
	[InlineData(HashAlgorithms.PBKDF2_SHA256)]
	[InlineData(HashAlgorithms.PBKDF2_SHA512)]
	public void HashPassword_WithModernAlgorithm_RoundTripsAndUsesRandomSalt(HashAlgorithms algorithm)
	{
		var first = PasswordHasher.HashPassword(Password, algorithm, Encoding.UTF8);
		var second = PasswordHasher.HashPassword(Password, algorithm, Encoding.UTF8);
		
		Assert.NotEqual(first, second);
		Assert.True(PasswordHasher.VerifyPassword(Password, first, algorithm, Encoding.UTF8));
		Assert.True(PasswordHasher.VerifyPassword(Password, second, algorithm, Encoding.UTF8));
	}
	
	#endregion
	
	#region Algorithm Switching
	
	[Theory]
	[InlineData(HashAlgorithms.ARGON2ID)]
	[InlineData(HashAlgorithms.PBKDF2_SHA256)]
	[InlineData(HashAlgorithms.PBKDF2_SHA512)]
	[InlineData(HashAlgorithms.SHA2_256)]
	[InlineData(HashAlgorithms.MD5)]
	public void VerifyPassword_WithPhcHash_IgnoresMembershipAlgorithm(HashAlgorithms membershipAlgorithm)
	{
		Assert.True(PasswordHasher.VerifyPassword(Password, Argon2idVector, membershipAlgorithm, Encoding.UTF8));
		Assert.True(PasswordHasher.VerifyPassword(Password, Pbkdf2Sha512Vector, membershipAlgorithm, Encoding.UTF8));
	}
	
	[Theory]
	[InlineData(HashAlgorithms.ARGON2ID)]
	[InlineData(HashAlgorithms.PBKDF2_SHA256)]
	[InlineData(HashAlgorithms.PBKDF2_SHA512)]
	public void VerifyPassword_WithLegacyHashAndModernMembershipAlgorithm_ReturnsFalse(HashAlgorithms membershipAlgorithm)
	{
		Assert.False(PasswordHasher.VerifyPassword(Password, Sha256OfPassword, membershipAlgorithm, Encoding.UTF8));
	}
	
	[Fact]
	public void VerifyPassword_WithLegacyHashAndMatchingLegacyAlgorithm_ReturnsTrue()
	{
		Assert.True(PasswordHasher.VerifyPassword(Password, Sha256OfPassword, HashAlgorithms.SHA2_256, Encoding.UTF8));
	}
	
	#endregion
	
	#region Malformed Hashes
	
	[Theory]
	[InlineData("$")]
	[InlineData("$argon2id$")]
	[InlineData("$argon2i$v=19$m=19456,t=2,p=1$RXJ0aXNBdXRoU2FsdCExNg$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc")]
	[InlineData("$argon2id$v=16$m=19456,t=2,p=1$RXJ0aXNBdXRoU2FsdCExNg$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc")]
	[InlineData("$argon2id$v=19$m=19456,t=2$RXJ0aXNBdXRoU2FsdCExNg$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc")]
	[InlineData("$argon2id$v=19$m=99999999,t=2,p=1$RXJ0aXNBdXRoU2FsdCExNg$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc")]
	[InlineData("$argon2id$v=19$m=19456,t=-2,p=1$RXJ0aXNBdXRoU2FsdCExNg$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc")]
	[InlineData("$argon2id$v=19$m=19456,t=2,p=1$not base64!$fbBQyhNR+kJrPcAQUBon1Sg6Erqte6DHJbnLH1xxVCc")]
	[InlineData("$argon2id$v=19$m=19456,t=2,p=1$RXJ0aXNBdXRoU2FsdCExNg$c2hvcnQ")]
	[InlineData("$pbkdf2-sha256$i=99999999999$RXJ0aXNBdXRoU2FsdCExNg$KHerChsPs4Ruu9BoHAgcQZwRsn9TwHuFh32Nm17nPTU")]
	[InlineData("$pbkdf2-sha256$iterations=600000$RXJ0aXNBdXRoU2FsdCExNg$KHerChsPs4Ruu9BoHAgcQZwRsn9TwHuFh32Nm17nPTU")]
	[InlineData("$pbkdf2-sha1$i=600000$RXJ0aXNBdXRoU2FsdCExNg$KHerChsPs4Ruu9BoHAgcQZwRsn9TwHuFh32Nm17nPTU")]
	[InlineData("$pbkdf2-sha256$i=600000$RXJ0aXNBdXRoU2FsdCExNg$KHerChsPs4Ruu9BoHAgcQZwRsn9TwHuFh32Nm17nPTU$extra")]
	public void VerifyPassword_WithMalformedOrUnsupportedPhcHash_ReturnsFalse(string storedHash)
	{
		Assert.False(PasswordHasher.VerifyPassword(Password, storedHash, HashAlgorithms.ARGON2ID, Encoding.UTF8));
	}
	
	#endregion
}
