using System.Security.Cryptography;
using System.Text;
using ErtisAuth.Core.Models.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;

namespace ErtisAuth.Infrastructure.Helpers;

/// <summary>
/// Hashes and verifies user passwords.
/// Legacy algorithms (MD5, SHA1, SHA2, SHA3) produce unsalted lowercase hex digests and are kept byte-for-byte compatible.
/// Modern algorithms (Argon2id, PBKDF2) produce salted, self-describing PHC strings, e.g.
/// <c>$argon2id$v=19$m=19456,t=2,p=1$&lt;salt&gt;$&lt;hash&gt;</c> or <c>$pbkdf2-sha256$i=600000$&lt;salt&gt;$&lt;hash&gt;</c>.
/// PHC strings are verified with the parameters they carry, independently of the membership's current algorithm.
/// </summary>
public static class PasswordHasher
{
	#region Constants
	
	private const string ARGON2ID_PHC_ID = "argon2id";
	private const string PBKDF2_SHA256_PHC_ID = "pbkdf2-sha256";
	private const string PBKDF2_SHA512_PHC_ID = "pbkdf2-sha512";
	
	private const int SALT_SIZE = 16;
	
	// OWASP Password Storage Cheat Sheet recommendations
	private const int ARGON2_MEMORY_KIB = 19456;
	private const int ARGON2_ITERATIONS = 2;
	private const int ARGON2_PARALLELISM = 1;
	private const int ARGON2_HASH_SIZE = 32;
	private const int PBKDF2_SHA256_ITERATIONS = 600_000;
	private const int PBKDF2_SHA512_ITERATIONS = 210_000;
	
	// Upper bounds for parameters read from stored hashes
	private const int MAX_ARGON2_MEMORY_KIB = 1024 * 1024;
	private const int MAX_ARGON2_ITERATIONS = 100;
	private const int MAX_ARGON2_PARALLELISM = 64;
	private const int MAX_PBKDF2_ITERATIONS = 10_000_000;
	private const int MIN_HASH_SIZE = 16;
	private const int MAX_HASH_SIZE = 128;
	
	#endregion
	
	#region Hash Methods
	
	public static string HashPassword(string password, HashAlgorithms algorithm, Encoding encoding)
	{
		var data = encoding.GetBytes(password);
		return algorithm switch
		{
			HashAlgorithms.ARGON2ID => HashArgon2id(data),
			HashAlgorithms.PBKDF2_SHA256 => HashPbkdf2(data, PBKDF2_SHA256_PHC_ID, HashAlgorithmName.SHA256, PBKDF2_SHA256_ITERATIONS, 32),
			HashAlgorithms.PBKDF2_SHA512 => HashPbkdf2(data, PBKDF2_SHA512_PHC_ID, HashAlgorithmName.SHA512, PBKDF2_SHA512_ITERATIONS, 64),
			_ => Convert.ToHexStringLower(ComputeLegacyDigest(data, algorithm))
		};
	}
	
	private static string HashArgon2id(byte[] password)
	{
		var salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
		var hash = ComputeArgon2id(password, salt, ARGON2_MEMORY_KIB, ARGON2_ITERATIONS, ARGON2_PARALLELISM, ARGON2_HASH_SIZE);
		return $"${ARGON2ID_PHC_ID}$v=19$m={ARGON2_MEMORY_KIB},t={ARGON2_ITERATIONS},p={ARGON2_PARALLELISM}${ToPhcBase64(salt)}${ToPhcBase64(hash)}";
	}
	
	private static string HashPbkdf2(byte[] password, string phcId, HashAlgorithmName hashAlgorithm, int iterations, int hashSize)
	{
		var salt = RandomNumberGenerator.GetBytes(SALT_SIZE);
		var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, hashSize);
		return $"${phcId}$i={iterations}${ToPhcBase64(salt)}${ToPhcBase64(hash)}";
	}
	
	private static byte[] ComputeArgon2id(byte[] password, byte[] salt, int memoryKiB, int iterations, int parallelism, int hashSize)
	{
		var parameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
			.WithVersion(Argon2Parameters.Version13)
			.WithMemoryAsKB(memoryKiB)
			.WithIterations(iterations)
			.WithParallelism(parallelism)
			.WithSalt(salt)
			.Build();
			
		var generator = new Argon2BytesGenerator();
		generator.Init(parameters);
		var hash = new byte[hashSize];
		generator.GenerateBytes(password, hash);
		return hash;
	}
	
	private static byte[] ComputeLegacyDigest(byte[] data, HashAlgorithms algorithm)
	{
		return algorithm switch
		{
			HashAlgorithms.MD5 => MD5.HashData(data),
			HashAlgorithms.SHA1 => SHA1.HashData(data),
			HashAlgorithms.SHA2_224 => ComputeDigest(new Sha224Digest(), data),
			HashAlgorithms.SHA2_256 => SHA256.HashData(data),
			HashAlgorithms.SHA2_384 => SHA384.HashData(data),
			HashAlgorithms.SHA2_512 => SHA512.HashData(data),
			HashAlgorithms.SHA2_512_224 => ComputeDigest(new Sha512tDigest(224), data),
			HashAlgorithms.SHA2_512_256 => ComputeDigest(new Sha512tDigest(256), data),
			HashAlgorithms.SHA3_224 => ComputeDigest(new Sha3Digest(224), data),
			HashAlgorithms.SHA3_256 => SHA3_256.IsSupported ? SHA3_256.HashData(data) : ComputeDigest(new Sha3Digest(256), data),
			HashAlgorithms.SHA3_384 => SHA3_384.IsSupported ? SHA3_384.HashData(data) : ComputeDigest(new Sha3Digest(384), data),
			HashAlgorithms.SHA3_512 => SHA3_512.IsSupported ? SHA3_512.HashData(data) : ComputeDigest(new Sha3Digest(512), data),
			_ => throw new NotSupportedException($"Not supported hash algorithm: {algorithm}")
		};
	}
	
	private static byte[] ComputeDigest(IDigest digest, byte[] data)
	{
		digest.BlockUpdate(data, 0, data.Length);
		var output = new byte[digest.GetDigestSize()];
		digest.DoFinal(output, 0);
		return output;
	}
	
	#endregion
	
	#region Verify Methods
	
	/// <summary>
	/// Verifies a password against a stored hash.
	/// PHC strings (starting with '$') are verified with their embedded parameters.
	/// Legacy hex digests are verified with the given (membership) algorithm using an exact, case-sensitive comparison.
	/// </summary>
	public static bool VerifyPassword(string password, string storedHash, HashAlgorithms algorithm, Encoding encoding)
	{
		var data = encoding.GetBytes(password);
		if (storedHash.StartsWith('$'))
		{
			return VerifyPhcHash(data, storedHash);
		}
		
		if (IsModernAlgorithm(algorithm))
		{
			// A legacy digest carries no algorithm information; it can only be verified with a legacy membership algorithm.
			return false;
		}
		
		var computedHash = Convert.ToHexStringLower(ComputeLegacyDigest(data, algorithm));
		return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computedHash), Encoding.ASCII.GetBytes(storedHash));
	}
	
	private static bool VerifyPhcHash(byte[] password, string storedHash)
	{
		// Expected layouts: $argon2id$v=19$m=..,t=..,p=..$salt$hash and $pbkdf2-sha*$i=..$salt$hash
		var segments = storedHash.Split('$');
		if (segments.Length < 5 || segments[0] != string.Empty)
		{
			return false;
		}
		
		try
		{
			switch (segments[1])
			{
				case ARGON2ID_PHC_ID when segments.Length == 6:
					return VerifyArgon2id(password, segments[2], segments[3], segments[4], segments[5]);
				case PBKDF2_SHA256_PHC_ID when segments.Length == 5:
					return VerifyPbkdf2(password, HashAlgorithmName.SHA256, segments[2], segments[3], segments[4]);
				case PBKDF2_SHA512_PHC_ID when segments.Length == 5:
					return VerifyPbkdf2(password, HashAlgorithmName.SHA512, segments[2], segments[3], segments[4]);
				default:
					return false;
			}
		}
		catch (FormatException)
		{
			return false;
		}
	}
	
	private static bool VerifyArgon2id(byte[] password, string versionSegment, string parametersSegment, string saltSegment, string hashSegment)
	{
		if (versionSegment != "v=19")
		{
			return false;
		}
		
		var parameters = ParsePhcParameters(parametersSegment);
		if (!TryGetParameter(parameters, "m", 8, MAX_ARGON2_MEMORY_KIB, out var memoryKiB) ||
			!TryGetParameter(parameters, "t", 1, MAX_ARGON2_ITERATIONS, out var iterations) ||
			!TryGetParameter(parameters, "p", 1, MAX_ARGON2_PARALLELISM, out var parallelism))
		{
			return false;
		}
		
		var salt = FromPhcBase64(saltSegment);
		var expectedHash = FromPhcBase64(hashSegment);
		if (expectedHash.Length is < MIN_HASH_SIZE or > MAX_HASH_SIZE)
		{
			return false;
		}
		
		var hash = ComputeArgon2id(password, salt, memoryKiB, iterations, parallelism, expectedHash.Length);
		return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
	}
	
	private static bool VerifyPbkdf2(byte[] password, HashAlgorithmName hashAlgorithm, string parametersSegment, string saltSegment, string hashSegment)
	{
		var parameters = ParsePhcParameters(parametersSegment);
		if (!TryGetParameter(parameters, "i", 1, MAX_PBKDF2_ITERATIONS, out var iterations))
		{
			return false;
		}
		
		var salt = FromPhcBase64(saltSegment);
		var expectedHash = FromPhcBase64(hashSegment);
		if (expectedHash.Length is < MIN_HASH_SIZE or > MAX_HASH_SIZE)
		{
			return false;
		}
		
		var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, expectedHash.Length);
		return CryptographicOperations.FixedTimeEquals(hash, expectedHash);
	}
	
	#endregion
	
	#region Helper Methods
	
	private static bool IsModernAlgorithm(HashAlgorithms algorithm)
	{
		return algorithm is HashAlgorithms.ARGON2ID or HashAlgorithms.PBKDF2_SHA256 or HashAlgorithms.PBKDF2_SHA512;
	}
	
	private static Dictionary<string, string> ParsePhcParameters(string segment)
	{
		var parameters = new Dictionary<string, string>();
		foreach (var pair in segment.Split(','))
		{
			var parts = pair.Split('=', 2);
			if (parts.Length == 2)
			{
				parameters[parts[0]] = parts[1];
			}
		}
		
		return parameters;
	}
	
	private static bool TryGetParameter(Dictionary<string, string> parameters, string name, int min, int max, out int value)
	{
		value = 0;
		return
			parameters.TryGetValue(name, out var raw) &&
			int.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out value) &&
			value >= min &&
			value <= max;
	}
	
	// PHC string format uses standard base64 without padding
	private static string ToPhcBase64(byte[] bytes)
	{
		return Convert.ToBase64String(bytes).TrimEnd('=');
	}
	
	private static byte[] FromPhcBase64(string value)
	{
		var padding = (4 - value.Length % 4) % 4;
		return Convert.FromBase64String(value + new string('=', padding));
	}
	
	#endregion
}
