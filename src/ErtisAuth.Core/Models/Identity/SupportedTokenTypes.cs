using System.Runtime.Serialization;

namespace ErtisAuth.Core.Models.Identity;

public enum SupportedTokenTypes
{
	[EnumMember(Value = "none")]
	None,
	
	[EnumMember(Value = "basic")]
	Basic,
	
	[EnumMember(Value = "bearer")]
	Bearer
}

public static class TokenTypeExtensions
{
	#region Methods
	
	/// <summary>
	/// Parses an authorization scheme, case-insensitively (RFC 7235); 'None' is not a scheme
	/// </summary>
	public static bool TryParseTokenType(string tokenType, out SupportedTokenTypes supportedTokenType)
	{
		if (string.Equals(tokenType, nameof(SupportedTokenTypes.Bearer), StringComparison.OrdinalIgnoreCase))
		{
			supportedTokenType = SupportedTokenTypes.Bearer;
			return true;
		}
		
		if (string.Equals(tokenType, nameof(SupportedTokenTypes.Basic), StringComparison.OrdinalIgnoreCase))
		{
			supportedTokenType = SupportedTokenTypes.Basic;
			return true;
		}
		
		supportedTokenType = SupportedTokenTypes.None;
		return false;
	}
	
	#endregion
}