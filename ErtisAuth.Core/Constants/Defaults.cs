using System.Text;
using ErtisAuth.Core.Models.Cryptography;

namespace ErtisAuth.Core.Constants;

public static class Defaults
{
	public const HashAlgorithms DEFAULT_HASH_ALGORITHM = HashAlgorithms.SHA2_256;
	public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
}