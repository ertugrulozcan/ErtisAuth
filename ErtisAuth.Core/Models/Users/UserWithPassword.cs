using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Users
{
	public class UserWithPassword : User
	{
		#region Properties
		
		[JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
		[JsonPropertyName("password")]
		[System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public string Password { get; set; }
		
		#endregion
	}
}