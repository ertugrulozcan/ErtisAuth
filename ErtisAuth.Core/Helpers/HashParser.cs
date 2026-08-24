using ErtisAuth.Core.Models.Cryptography;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable OutParameterValueIsAlwaysDiscarded.Global
namespace ErtisAuth.Core.Helpers;

public static class HashParser
{
	public static bool TryParseHashAlgorithm(string algorithmName, out HashAlgorithms algorithm, out int outputBitSize, out int stateSize)
	{
		if (string.IsNullOrEmpty(algorithmName))
		{
			algorithm = default;
			outputBitSize = 0;
			stateSize = 0;
			return false;
		}
		
		algorithmName = algorithmName.ToUpper();
		var segments = algorithmName.Split('-');
		var algorithmNameSegment = segments.FirstOrDefault();
		var specificOutputSize = 0;
		var specificStateSize = 0;
		
		if (segments.Length > 2)
		{
			int.TryParse(segments[1], out specificStateSize);
			int.TryParse(segments[2], out specificOutputSize);
		}
		else if (segments.Length > 1)
		{
			if (segments[1].Contains('/'))
			{
				int.TryParse(segments[1].Split('/')[0], out specificStateSize);
				int.TryParse(segments[1].Split('/')[1], out specificOutputSize);
			}
			else
			{
				int.TryParse(segments[1], out specificOutputSize);
			}
		}
		
		switch (algorithmNameSegment)
		{
			case "MD5":
				algorithm = HashAlgorithms.MD5;
				outputBitSize = specificOutputSize > 0 ? specificOutputSize : 128;
				stateSize = specificStateSize > 0 ? specificStateSize : 128;
				return true;
			case "SHA":
				outputBitSize = specificOutputSize;
				stateSize = 0;
				if (specificOutputSize == 224)
				{
					algorithm = HashAlgorithms.SHA2_224;
					return true;
				}
				else if (specificOutputSize == 256)
				{
					algorithm = HashAlgorithms.SHA2_256;
					return true;
				}
				else if (specificOutputSize == 384)
				{
					algorithm = HashAlgorithms.SHA2_384;
					return true;
				}
				else if (specificOutputSize == 512)
				{
					algorithm = HashAlgorithms.SHA2_512;
					return true;
				}
				else
				{
					algorithm = default;
					return false;
				}
			case "SHA224":
				algorithm = HashAlgorithms.SHA2_224;
				outputBitSize = 224;
				stateSize = 0;
				return true;
			case "SHA256":
				algorithm = HashAlgorithms.SHA2_256;
				outputBitSize = 256;
				stateSize = 0;
				return true;
			case "SHA384":
				algorithm = HashAlgorithms.SHA2_384;
				outputBitSize = 384;
				stateSize = 0;
				return true;
			case "SHA512":
				algorithm = HashAlgorithms.SHA2_512;
				outputBitSize = 512;
				stateSize = 0;
				return true;
			case "SHA0":
				algorithm = HashAlgorithms.SHA0;
				outputBitSize = specificOutputSize > 0 ? specificOutputSize : 160;
				stateSize = specificStateSize > 0 ? specificStateSize : 160;
				return true;
			case "SHA1":
				algorithm = HashAlgorithms.SHA1;
				outputBitSize = specificOutputSize > 0 ? specificOutputSize : 160;
				stateSize = specificStateSize > 0 ? specificStateSize : 160;
				return true;
			case "SHA2" when specificOutputSize == 224:
			{
				algorithm = HashAlgorithms.SHA2_224;
				if (specificStateSize == 512)
				{
					algorithm = HashAlgorithms.SHA2_512_224;
				}
				
				outputBitSize = 224;
				stateSize = specificStateSize > 0 ? specificStateSize : 256;
				return true;
			}
			case "SHA2" when specificOutputSize == 256:
			{
				algorithm = HashAlgorithms.SHA2_256;
				if (specificStateSize == 512)
				{
					algorithm = HashAlgorithms.SHA2_512_256;
				}
				
				outputBitSize = 256;
				stateSize = specificStateSize > 0 ? specificStateSize : 256;
				return true;
			}
			case "SHA2" when specificOutputSize == 384:
				algorithm = HashAlgorithms.SHA2_384;
				outputBitSize = 384;
				stateSize = specificStateSize > 0 ? specificStateSize : 512;
				return true;
			case "SHA2" when specificOutputSize == 512:
				algorithm = HashAlgorithms.SHA2_512;
				outputBitSize = 512;
				stateSize = specificStateSize > 0 ? specificStateSize : 512;
				return true;
			case "SHA2":
				algorithm = default;
				outputBitSize = 0;
				stateSize = 0;
				return false;
			case "SHA3" when specificOutputSize == 224:
				algorithm = HashAlgorithms.SHA3_224;
				outputBitSize = 224;
				stateSize = specificStateSize > 0 ? specificStateSize : 1600;
				return true;
			case "SHA3" when specificOutputSize == 256:
				algorithm = HashAlgorithms.SHA3_256;
				outputBitSize = 256;
				stateSize = specificStateSize > 0 ? specificStateSize : 1600;
				return true;
			case "SHA3" when specificOutputSize == 384:
				algorithm = HashAlgorithms.SHA3_384;
				outputBitSize = 384;
				stateSize = specificStateSize > 0 ? specificStateSize : 1600;
				return true;
			case "SHA3" when specificOutputSize == 512:
				algorithm = HashAlgorithms.SHA3_512;
				outputBitSize = 512;
				stateSize = specificStateSize > 0 ? specificStateSize : 1600;
				return true;
			case "SHA3":
				algorithm = default;
				outputBitSize = 0;
				stateSize = 0;
				return false;
			default:
				algorithm = default;
				outputBitSize = 0;
				stateSize = 0;
				return false;
		}
	}
}