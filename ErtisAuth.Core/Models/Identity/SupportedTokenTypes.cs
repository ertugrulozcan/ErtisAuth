using System.Runtime.Serialization;

namespace ErtisAuth.Core.Models.Identity
{
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

		public static bool TryParseTokenType(string tokenType, out SupportedTokenTypes supportedTokenType)
		{
			switch (tokenType)
			{
				case "Bearer":
					supportedTokenType = SupportedTokenTypes.Bearer;
					return true;
				case "Basic":
					supportedTokenType = SupportedTokenTypes.Basic;
					return true;
				default:
					supportedTokenType = SupportedTokenTypes.None;
					return false;
			}
		}

		#endregion
	}
}