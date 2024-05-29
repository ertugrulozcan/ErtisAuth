using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public interface ITokenValidationResult
	{
		[JsonProperty("verified")]
		[JsonPropertyName("verified")]
		bool IsValidated { get; }
		
		[JsonProperty("token")]
		[JsonPropertyName("token")]
		string Token { get; }
	}
}