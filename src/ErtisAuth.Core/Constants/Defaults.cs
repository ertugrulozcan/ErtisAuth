using System.Text;
using Ertis.MongoDB.Queries;
using ErtisAuth.Core.Models.Cryptography;

namespace ErtisAuth.Core.Constants;

public static class Defaults
{
	#region Constants
	
	/// <summary>
	/// Suggested algorithm for new memberships. It is never applied implicitly; memberships must declare their own hash algorithm.
	/// </summary>
	private const HashAlgorithms RECOMMENDED_HASH_ALGORITHM = HashAlgorithms.ARGON2ID;
	
	private static readonly Encoding DEFAULT_ENCODING = Encoding.UTF8;
	
	private static readonly TextSearchLanguage DEFAULT_LOCALE = TextSearchLanguage.None;
	
	#endregion
	
	#region Methods
	
	public static string GetDefaultHashAlgorithm()
	{
		return RECOMMENDED_HASH_ALGORITHM.ToString().Replace('_', '-');
	}
	
	public static Encoding GetDefaultEncoding()
	{
		return DEFAULT_ENCODING;
	}
	
	public static string GetDefaultEncodingName()
	{
		return GetDefaultEncoding().HeaderName.ToUpperInvariant();
	}
	
	public static string GetDefaultLocale()
	{
		return DEFAULT_LOCALE.ISO6391Code;
	}
	
	#endregion
}