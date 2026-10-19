using System.Globalization;
using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

public class LoginNameHelperTests
{
	#region Normalize
	
	[Theory]
	[InlineData("Jane.Doe@Example.COM", "jane.doe@example.com")]
	[InlineData("  jane  ", "jane")]
	[InlineData("\tJANE\n", "jane")]
	[InlineData("jane", "jane")]
	public void Normalize_TrimsAndLowerCases(string value, string expected)
	{
		Assert.Equal(expected, LoginNameHelper.Normalize(value));
	}
	
	/// <summary>
	/// A decomposed character (e + combining acute accent) and its precomposed form (é) become the same value.
	/// </summary>
	[Fact]
	public void Normalize_ComposesDecomposedCharacters()
	{
		Assert.Equal("caf\u00e9", LoginNameHelper.Normalize("cafe\u0301"));
		Assert.Equal("caf\u00e9", LoginNameHelper.Normalize("CAF\u00c9"));
	}
	
	/// <summary>
	/// Lower casing doesn't depend on the culture of the server (e.g. Turkish: 'I' -> 'ı' and 'İ' -> 'i').
	/// </summary>
	[Fact]
	public void Normalize_IsCultureInvariant()
	{
		var culture = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
			
			Assert.Equal("ilker", LoginNameHelper.Normalize("ILKER"));
			Assert.Equal("\u0131\u015f\u0131k", LoginNameHelper.Normalize("\u0131\u015f\u0131k"));
		}
		finally
		{
			CultureInfo.CurrentCulture = culture;
		}
	}
	
	/// <summary>
	/// Full width characters are not mapped to their ASCII forms (NFC, not NFKC).
	/// </summary>
	[Fact]
	public void Normalize_KeepsCompatibilityCharacters()
	{
		Assert.Equal("\uff41dmin", LoginNameHelper.Normalize("\uff21dmin"));
	}
	
	[Theory]
	[InlineData(null, null)]
	[InlineData("", "")]
	[InlineData("   ", "")]
	public void Normalize_WithoutValue_ReturnsItEmpty(string? value, string? expected)
	{
		Assert.Equal(expected, LoginNameHelper.Normalize(value));
	}
	
	#endregion
}
