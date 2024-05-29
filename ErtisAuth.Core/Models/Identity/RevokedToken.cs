using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public class RevokedToken : MembershipBoundedResource
	{
		#region Properties

		[JsonProperty("token")]
		[JsonPropertyName("token")]
		public string Token { get; set; }
		
		[JsonProperty("user_id")]
		[JsonPropertyName("user_id")]
		public string UserId { get; set; }
		
		[JsonProperty("username")]
		[JsonPropertyName("username")]
		public string UserName { get; set; }
		
		[JsonProperty("email_address")]
		[JsonPropertyName("email_address")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("first_name")]
		[JsonPropertyName("first_name")]
		public string FirstName { get; set; }
		
		[JsonProperty("last_name")]
		[JsonPropertyName("last_name")]
		public string LastName { get; set; }
		
		[JsonProperty("token_type")]
		[JsonPropertyName("token_type")]
		public string TokenType { get; set; }
		
		[JsonProperty("revoked_at")]
		[JsonPropertyName("revoked_at")]
		public DateTime RevokedAt { get; set; }

		#endregion
	}
}