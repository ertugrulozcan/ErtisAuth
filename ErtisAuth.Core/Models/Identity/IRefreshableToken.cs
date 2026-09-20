using System.Text.Json.Serialization;
using Newtonsoft.Json;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Core.Models.Identity;

public interface IRefreshableToken
{
	[JsonProperty("refresh_token")]
	[JsonPropertyName("refresh_token")]
	string? RefreshToken { get; }
	
	[NewtonsoftJsonIgnore]
	[System.Text.Json.Serialization.JsonIgnore]
	TimeSpan RefreshExpiresIn { get; }
}