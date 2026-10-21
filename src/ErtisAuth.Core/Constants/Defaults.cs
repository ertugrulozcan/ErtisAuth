using System.Text;
using Ertis.MongoDB.Queries;
using ErtisAuth.Core.Models.Cryptography;

namespace ErtisAuth.Core.Constants;

public static class Defaults
{
	#region Constants
	
	public static readonly TimeSpan TOKEN_TTL = TimeSpan.FromHours(12);
	
	public static readonly TimeSpan REFRESH_TOKEN_TTL = TimeSpan.FromHours(1);
	
	public static readonly TimeSpan RESET_PASSWORD_TOKEN_TTL = TimeSpan.FromHours(2);
	
	public static readonly TimeSpan ACTIVATION_TOKEN_TTL = TimeSpan.FromHours(72);
	
	public static readonly TimeSpan SCOPED_TOKEN_TTL = TimeSpan.FromHours(12);
	
	public static readonly TimeSpan OTP_TTL = TimeSpan.FromHours(2);
	
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