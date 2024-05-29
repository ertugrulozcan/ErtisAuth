using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public interface IUtilizer
	{
		#region Properties

		[JsonProperty("_id")]
		[JsonPropertyName("_id")]
		string Id { get; set; }
		
		[JsonProperty("role")]
		[JsonPropertyName("role")]
		string Role { get; set; }
		
		[JsonProperty("permissions")]
		[JsonPropertyName("permissions")]
		IEnumerable<string> Permissions { get; set; }
		
		[JsonProperty("forbidden")]
		[JsonPropertyName("forbidden")]
		IEnumerable<string> Forbidden { get; set; }
		
		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		Utilizer.UtilizerType UtilizerType { get; }
		
		[JsonProperty("membership_id")]
		[JsonPropertyName("membership_id")]
		string MembershipId { get; set; }

		#endregion
	}
}