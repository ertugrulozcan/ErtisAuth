using System.Text;
using ErtisAuth.Core.Models.Cryptography;

namespace ErtisAuth.Core.Constants;

public static class Defaults
{
	#region Constants
	
	/// <summary>
	/// Suggested algorithm for new memberships. It is never applied implicitly; memberships must declare their own hash algorithm.
	/// </summary>
	public const HashAlgorithms RECOMMENDED_HASH_ALGORITHM = HashAlgorithms.ARGON2ID;
	
	public static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
	
	#endregion
}