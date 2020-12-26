using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public interface IRefreshableToken
	{
		[JsonProperty("refresh_token")]
		string RefreshToken { get; }
		
		[JsonIgnore]
		TimeSpan RefreshExpiresIn { get; }
	}
}