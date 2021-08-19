using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public class ActiveToken
	{
		#region Properties

		[JsonProperty("access_token")]
		public string AccessToken { get; set; }
		
		[JsonProperty("refresh_token")]
		public string RefreshToken { get; set; }

		[JsonProperty("expires_in")]
		public int ExpiresIn { get; set; }
		
		[JsonProperty("refresh_token_expires_in")]
		public int RefreshTokenExpiresIn { get; set; }
		
		[JsonProperty("token_type")]
		public string TokenType { get; set; }

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; set; }
		
		[JsonProperty("user_id")]
		public string UserId { get; set; }
		
		[JsonProperty("membership_id")]
		public string MembershipId { get; set; }

		[JsonProperty("expire_time")]
		public DateTime ExpireTime => this.CreatedAt.Add(TimeSpan.FromSeconds(this.ExpiresIn));

		#endregion
	}
}