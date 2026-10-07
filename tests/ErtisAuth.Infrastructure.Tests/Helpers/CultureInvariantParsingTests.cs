using System.Globalization;
using ErtisAuth.Core.Helpers;
using ErtisAuth.Core.Models.Cryptography;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// The names are parsed culture invariantly: in tr-TR the uppercase of 'i' is the dotted 'İ' and the lowercase of 'I' is the dotless 'ı'.
/// </summary>
public class CultureInvariantParsingTests
{
	#region Helpers
	
	private static T InCulture<T>(string cultureName, Func<T> func)
	{
		var previousCulture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo(cultureName);
			return func();
		}
		finally
		{
			CultureInfo.CurrentCulture = previousCulture;
		}
	}
	
	#endregion
	
	#region Methods
	
	[Theory]
	[InlineData("en-US")]
	[InlineData("tr-TR")]
	public void HashParser_ParsesALowercaseAlgorithmName(string cultureName)
	{
		var isParsed = InCulture(cultureName, () => HashParser.TryParseHashAlgorithm("argon2id", out var algorithm, out _, out _) && algorithm == HashAlgorithms.ARGON2ID);
		
		Assert.True(isParsed);
	}
	
	[Theory]
	[InlineData("en-US", "APPLICATION")]
	[InlineData("tr-TR", "APPLICATION")]
	[InlineData("tr-TR", "application")]
	public void Utilizer_ParsesTheTypeInAnyCase(string cultureName, string type)
	{
		Assert.Equal(Utilizer.UtilizerType.Application, InCulture(cultureName, () => Utilizer.ParseType(type)));
	}
	
	#endregion
}
