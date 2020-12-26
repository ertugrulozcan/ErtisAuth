using System.Runtime.Serialization;

namespace ErtisAuth.Core.Models.Identity
{
	public enum SupportedTokenTypes
	{
		[EnumMember(Value = "basic")]
		Basic,
		
		[EnumMember(Value = "bearer")]
		Bearer
	}
}