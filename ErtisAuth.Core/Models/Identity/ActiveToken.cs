using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public class ActiveToken : MembershipBoundedResource
	{
		#region Properties

		[JsonProperty("access_token")]
		[JsonPropertyName("access_token")]
		public string AccessToken { get; set; }
		
		[JsonProperty("refresh_token")]
		[JsonPropertyName("refresh_token")]
		public string RefreshToken { get; set; }

		[JsonProperty("expires_in")]
		[JsonPropertyName("expires_in")]
		public int ExpiresIn { get; set; }
		
		[JsonProperty("refresh_token_expires_in")]
		[JsonPropertyName("refresh_token_expires_in")]
		public int RefreshTokenExpiresIn { get; set; }
		
		[JsonProperty("token_type")]
		[JsonPropertyName("token_type")]
		public string TokenType { get; set; }

		[JsonProperty("created_at")]
		[JsonPropertyName("created_at")]
		public DateTime CreatedAt { get; set; }
		
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
		
		[JsonProperty("expire_time")]
		[JsonPropertyName("expire_time")]
		public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));
		
		[JsonProperty("client_info")]
		[JsonPropertyName("client_info")]
		public ClientInfo ClientInfo { get; set; }

		#endregion
	}
}