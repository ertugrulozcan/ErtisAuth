using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public interface IRefreshableToken
	{
		[JsonProperty("refresh_token")]
		[JsonPropertyName("refresh_token")]
		string RefreshToken { get; }
		
		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		TimeSpan RefreshExpiresIn { get; }
	}
}