using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public interface ITokenValidationResult
	{
		[JsonProperty("verified")]
		bool IsValidated { get; }
		
		[JsonProperty("token")]
		string Token { get; }
	}
}