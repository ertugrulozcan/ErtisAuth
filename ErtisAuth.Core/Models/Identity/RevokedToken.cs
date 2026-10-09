using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public class RevokedToken : MembershipBoundedResource
	{
		#region Properties

		[JsonProperty("token")]
		public string Token { get; set; }
		
		[JsonProperty("user_id")]
		public string UserId { get; set; }
		
		[JsonProperty("username")]
		public string UserName { get; set; }
		
		[JsonProperty("email_address")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("first_name")]
		public string FirstName { get; set; }
		
		[JsonProperty("last_name")]
		public string LastName { get; set; }
		
		[JsonProperty("token_type")]
		public string TokenType { get; set; }
		
		[JsonProperty("revoked_at")]
		public DateTime RevokedAt { get; set; }

		#endregion
	}
}