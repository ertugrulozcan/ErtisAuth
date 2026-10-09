using System.Security.Cryptography;
using System.Text;

namespace ErtisAuth.Infrastructure.Helpers;

/// <summary>
/// Generates and verifies application secrets used in Basic tokens (applicationId:secret).
/// Secrets are high-entropy random values, so a single SHA-256 is enough to store them; a slow password hash
/// would only add cost to every Basic token verification.
/// </summary>
public static class ApplicationSecretHelper
{
	#region Constants
	
	private const int SecretSize = 32;
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Generates a new random secret. The base64url alphabet does not contain ':', so the secret never breaks the Basic token format.
	/// </summary>
	public static string GenerateSecret()
	{
		return Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretSize))
			.TrimEnd('=')
			.Replace('+', '-')
			.Replace('/', '_');
	}
	
	public static string HashSecret(string secret)
	{
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
	}
	
	/// <summary>
	/// Compares the hash of the given secret with the stored hash in constant time.
	/// Both sides are fixed-size hashes, so neither the secret nor its length can be inferred from response times.
	/// </summary>
	public static bool VerifySecret(string secret, string? secretHash)
	{
		if (string.IsNullOrEmpty(secretHash))
		{
			return false;
		}
		
		byte[] expectedHash;
		try
		{
			expectedHash = Convert.FromHexString(secretHash);
		}
		catch (FormatException)
		{
			return false;
		}
		
		var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
		return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
	}
	
	#endregion
}
