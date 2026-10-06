using System.Security.Cryptography;
using System.Text;

namespace ErtisAuth.Infrastructure.Helpers;

/// <summary>
/// Short codes a user types (one-time passwords, token codes), from a cryptographically secure random source.
/// </summary>
public static class RandomCodeGenerator
{
	#region Constants
	
	private static readonly char[] Letters = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z' };
	private static readonly char[] Digits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

	/// <summary>
	/// Characters which are easily confused with each other when letters and digits are mixed (0/O, 1/I) are left out.
	/// </summary>
	private static readonly char[] AmbiguousChars = { '0', 'O', '1', 'I' };
	private static readonly char[] AllChars = Letters.Concat(Digits).Except(AmbiguousChars).ToArray();
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Uppercase letters and/or digits; both when the policy allows neither. Mixed codes leave out the ambiguous characters.
	/// </summary>
	public static string Generate(int length, bool containsLetters, bool containsDigits)
	{
		var chars = AllChars;
		var onlyDigits = false;
		if (containsDigits && !containsLetters)
		{
			chars = Digits;
			onlyDigits = true;
		}
		else if (containsLetters && !containsDigits)
		{
			chars = Letters;
		}
		
		var stringBuilder = new StringBuilder();
		for (var i = 0; i < length; i++)
		{
			// A digits-only code doesn't start with 0, so it survives being handled as a number
			var character = onlyDigits && i == 0
				? Digits[RandomNumberGenerator.GetInt32(1, Digits.Length)]
				: chars[RandomNumberGenerator.GetInt32(chars.Length)];
			
			stringBuilder.Append(character);
		}
		
		return stringBuilder.ToString();
	}
	
	#endregion
}