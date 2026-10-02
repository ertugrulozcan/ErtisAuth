using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Infrastructure.Tests.Helpers;

/// <summary>
/// The authorization scheme of the Authorization header is case-insensitive (RFC 7235): it is read in any case and given with its canonical name.
/// </summary>
public class AuthorizationSchemeTests
{
	#region Methods
	
	[Theory]
	[InlineData("Bearer abc", "Bearer")]
	[InlineData("bearer abc", "Bearer")]
	[InlineData("BEARER abc", "Bearer")]
	[InlineData("Basic abc", "Basic")]
	[InlineData("basic abc", "Basic")]
	public void ExtractToken_ReadsTheSchemeInAnyCase(string authorizationHeader, string expectedTokenType)
	{
		var token = TokenBase.ExtractToken(authorizationHeader, out var tokenType);
		
		Assert.Equal("abc", token);
		Assert.Equal(expectedTokenType, tokenType);
	}
	
	[Theory]
	[InlineData("None abc")]
	[InlineData("Digest abc")]
	public void ExtractToken_WithUnsupportedScheme_Throws(string authorizationHeader)
	{
		var exception = Assert.Throws<ErtisAuthException>(() => TokenBase.ExtractToken(authorizationHeader, out _));
		
		Assert.Equal("TokenTypeNotSupported", exception.ErrorCode);
	}
	
	[Theory]
	[InlineData("bEaReR", SupportedTokenTypes.Bearer)]
	[InlineData("BASIC", SupportedTokenTypes.Basic)]
	public void TryParseTokenType_ParsesTheSchemeInAnyCase(string scheme, SupportedTokenTypes expected)
	{
		Assert.True(TokenTypeExtensions.TryParseTokenType(scheme, out var tokenType));
		Assert.Equal(expected, tokenType);
	}
	
	#endregion
}
