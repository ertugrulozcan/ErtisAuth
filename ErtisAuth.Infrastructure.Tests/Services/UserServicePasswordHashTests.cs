using ErtisAuth.Core.Exceptions;
using ErtisAuth.Infrastructure.Tests.Helpers;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// Characterization tests for the legacy (unsalted) password hashing behavior.
/// Expected values are computed independently (Python hashlib) and must never change,
/// otherwise existing users can no longer sign in.
/// </summary>
public class UserServicePasswordHashTests
{
	#region Constants
	
	private const string Password = "P@ssw0rd!";
	
	private const string UnicodePassword = "Şifre-Güçlü-ığüşöç";
	
	private const string Sha256OfPassword = "0e44ce7308af2b3de5232e4616403ce7d49ba2aec83f79c196409556422a4927";
	
	#endregion
	
	#region Algorithm Vectors
	
	public static TheoryData<string, string> AlgorithmVectors => new()
	{
		{ "MD5", "8a24367a1f46c141048752f2d5bbd14b" },
		{ "SHA1", "076d3e6c4b9f654b5b220b9045b7458ab6b4cbc6" },
		{ "SHA2-224", "836316ea674903fc9cafc01fd9c171a26d12aa7061b057f514b1e192" },
		{ "SHA2-256", Sha256OfPassword },
		{ "SHA2-384", "4339855eb64e55fa3115e1f2349825c99f2826e98e3f93b6c5d4dbd627de5ef34dbc4c64cb5ca8c35d61d260491a19ac" },
		{ "SHA2-512", "9a585872fc4a94ba2fe0f7e9625bcfccc1050dab20b56df35a1c8915a3d8325616c0172284c2f4fe9471667c9c4f1fbdc0d371684caee74cdf9d39d82383a383" },
		{ "SHA2-512-224", "7f5f8e88e87e95eb84bb4cd061550e5be625461db6aff4b5b1f4e18f" },
		{ "SHA2-512-256", "f85c8eeee9d3e814996f813ce1b89f144f0737b36813e7eccf0f8373260e4676" },
		{ "SHA3-224", "13d8f998c8aa48fce61e846549808e432e26bfae11f117f2d704ea2e" },
		{ "SHA3-256", "a19c7b7f7bb88e4baab4199d6d60efd91b2ce26f3b22b616d6997517e11dfcdc" },
		{ "SHA3-384", "24e0d2682c3431a9dba62b60c86ec9de79ca784458137fea84dd4a5b481a1b6131bce8c1ff0f2e6457f817e3bdf8feb0" },
		{ "SHA3-512", "a3cba91b5f7abc3d35e3e0603caba9ff85f36ccae7e3f5901cf6fa58186fe587dada0dc90fdffc35ab5383080d84afa93a47138f9735d1fee4bea1ca4ab74157" },
	};
	
	[Theory]
	[MemberData(nameof(AlgorithmVectors))]
	public void CalculatePasswordHash_WithLegacyAlgorithm_ReturnsKnownLowercaseHexDigest(string hashAlgorithm, string expectedHash)
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		
		var hash = userService.CalculatePasswordHash(membership, Password);
		
		Assert.Equal(expectedHash, hash);
	}
	
	#endregion
	
	#region Algorithm Name Parsing
	
	[Theory]
	[InlineData("SHA2-256")]
	[InlineData("sha2-256")]
	[InlineData("SHA-256")]
	[InlineData("SHA256")]
	public void CalculatePasswordHash_WithSha256Aliases_UsesSha256(string hashAlgorithm)
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		
		var hash = userService.CalculatePasswordHash(membership, Password);
		
		Assert.Equal(Sha256OfPassword, hash);
	}
	
	[Theory]
	[InlineData("SHA2-512/256")]
	[InlineData("SHA2-512-256")]
	public void CalculatePasswordHash_WithTruncatedSha512Aliases_UsesSha512_256(string hashAlgorithm)
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		
		var hash = userService.CalculatePasswordHash(membership, Password);
		
		Assert.Equal("f85c8eeee9d3e814996f813ce1b89f144f0737b36813e7eccf0f8373260e4676", hash);
	}
	
	/// <summary>
	/// Memberships must declare a valid hash algorithm; there is no implicit default anymore.
	/// Note that enum-style names with underscores (e.g. "SHA3_512") are not valid algorithm names.
	/// </summary>
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("UNKNOWN")]
	[InlineData("SHA2")]
	[InlineData("SHA3")]
	[InlineData("SHA-1024")]
	[InlineData("SHA3_512")]
	[InlineData("SHA2_512")]
	[InlineData("ARGON2")]
	[InlineData("PBKDF2")]
	[InlineData("PBKDF2-SHA1")]
	public void CalculatePasswordHash_WithMissingOrUnparsableAlgorithm_ThrowsMembershipHashAlgorithmInvalid(string? hashAlgorithm)
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		
		var exception = Assert.Throws<ErtisAuthException>(() => userService.CalculatePasswordHash(membership, Password));
		
		Assert.Equal("MembershipHashAlgorithmInvalid", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData("ARGON2ID", "$argon2id$")]
	[InlineData("argon2id", "$argon2id$")]
	[InlineData("PBKDF2-SHA256", "$pbkdf2-sha256$")]
	[InlineData("pbkdf2-sha512", "$pbkdf2-sha512$")]
	public void CalculatePasswordHash_WithModernAlgorithm_ReturnsVerifiablePhcString(string hashAlgorithm, string expectedPrefix)
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership(hashAlgorithm);
		
		var hash = userService.CalculatePasswordHash(membership, Password);
		
		Assert.StartsWith(expectedPrefix, hash);
		Assert.True(userService.VerifyPassword(membership, Password, hash));
	}
	
	#endregion
	
	#region Encodings
	
	[Theory]
	[InlineData(null, "e82a4868c1cc7b93616db124e4960c68225e803fa87348fe4dead647f37f0654")]
	[InlineData("utf-8", "e82a4868c1cc7b93616db124e4960c68225e803fa87348fe4dead647f37f0654")]
	[InlineData("UTF-8", "e82a4868c1cc7b93616db124e4960c68225e803fa87348fe4dead647f37f0654")]
	[InlineData("unknown-encoding", "e82a4868c1cc7b93616db124e4960c68225e803fa87348fe4dead647f37f0654")]
	[InlineData("utf-16", "b718810d2d4f20ec54e9c0856c1cfe72f3d80c230c4e39168c36fe16c4bb5380")]
	[InlineData("us-ascii", "054d9ad456ff81578adba062cba2660529dcc050ab9e24a3dbf476160e67a126")]
	// iso-8859-1 uses best-fit fallback for unmappable characters ("Şifre-Güçlü-ığüşöç" -> "Sifre-Güçlü-igüsöç"), while us-ascii replaces them with '?'.
	[InlineData("iso-8859-1", "792e283a67abf529504f2a36399cecdbd2dca1fef986bb6ea5a559a06bf74d32")]
	public void CalculatePasswordHash_WithMembershipEncoding_EncodesPasswordBeforeHashing(string? defaultEncoding, string expectedHash)
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership("SHA2-256", defaultEncoding);
		
		var hash = userService.CalculatePasswordHash(membership, UnicodePassword);
		
		Assert.Equal(expectedHash, hash);
	}
	
	#endregion
	
	#region Edge Cases
	
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public void CalculatePasswordHash_WithNullOrEmptyPassword_ReturnsInputUnchanged(string? password)
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		
		var hash = userService.CalculatePasswordHash(membership, password!);
		
		Assert.Equal(password, hash);
	}
	
	[Fact]
	public void CalculatePasswordHash_WithWhitespacePassword_HashesItAsIs()
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		
		var hash = userService.CalculatePasswordHash(membership, " ");
		
		Assert.Equal("36a9e7f1c95b82ffb99743e0c5c4ce95d83c9a430aac59f84ef3cbfab6145068", hash);
	}
	
	[Fact]
	public void CalculatePasswordHash_CalledTwice_IsDeterministic()
	{
		var userService = TestServiceFactory.CreateUserService();
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		
		var first = userService.CalculatePasswordHash(membership, Password);
		var second = userService.CalculatePasswordHash(membership, Password);
		
		Assert.Equal(first, second);
	}
	
	#endregion
}
