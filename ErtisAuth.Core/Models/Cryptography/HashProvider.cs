using System.Security.Cryptography;
using System.Text;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Core.Models.Cryptography;

public class HashProvider
{
	/*
	 * Supported Hash Algorithms
	 * ---------------------------
	 * MD5 (MD5-128)
	 * SHA0 (SHA0-160)
	 * SHA1 (SHA1-160)
	 * SHA2-224 (SHA-224)
	 * SHA2-256 (SHA-256)
	 * SHA2-384 (SHA-384)
	 * SHA2-512 (SHA-512)
	 * SHA2-512/224 (SHA-512/224)
	 * SHA2-512/256 (SHA-512/256)
	 * SHA3-224
	 * SHA3-256
	 * SHA3-384
	 * SHA3-512
	*/
	
	#region Methods
	
	public string Hash(string message, HashAlgorithms algorithm, Encoding encoding)
	{
		HashAlgorithm hashAlgorithm;
		switch (algorithm)
		{
			case HashAlgorithms.MD5:
				hashAlgorithm = MD5.Create();
				break;
			case HashAlgorithms.SHA0:
				throw new NotSupportedException("Not supported hash algorithm :(");
			case HashAlgorithms.SHA1:
				hashAlgorithm = SHA1.Create();
				break;
			case HashAlgorithms.SHA2_224:
				throw new NotSupportedException("Not supported hash algorithm :(");
			case HashAlgorithms.SHA2_256:
				hashAlgorithm = SHA256.Create();
				break;
			case HashAlgorithms.SHA2_384:
				hashAlgorithm = SHA384.Create();
				break;
			case HashAlgorithms.SHA2_512:
				hashAlgorithm = SHA512.Create();
				break;
			case HashAlgorithms.SHA2_512_224:
				throw new NotSupportedException("Not supported hash algorithm :(");
			case HashAlgorithms.SHA2_512_256:
				throw new NotSupportedException("Not supported hash algorithm :(");
			case HashAlgorithms.SHA3_224:
				throw new NotSupportedException("Not supported hash algorithm :(");
			case HashAlgorithms.SHA3_256:
				throw new NotSupportedException("Not supported hash algorithm :(");
			case HashAlgorithms.SHA3_384:
				throw new NotSupportedException("Not supported hash algorithm :(");
			case HashAlgorithms.SHA3_512:
				throw new NotSupportedException("Not supported hash algorithm :(");
			default:
				throw new NotSupportedException("Not supported hash algorithm :(");
		}
		
		var bytes = hashAlgorithm.ComputeHash(encoding.GetBytes(message));
		var hash = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower();
		return hash;
	}
	
	#endregion
}