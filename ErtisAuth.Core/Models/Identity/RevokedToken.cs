using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public class RevokedToken : ResourceBase
	{
		#region Properties

		[JsonProperty("token")]
		public string Token { get; set; }
		
		[JsonProperty("user_id")]
		public string UserId { get; set; }
		
		[JsonProperty("revoked_at")]
		public DateTime RevokedAt { get; set; }

		#endregion
	}
}