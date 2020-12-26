using System;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public abstract class TokenBase
	{
		#region Properties

		[JsonProperty("access_token")]
		public string AccessToken { get; set; }
		
		[JsonProperty("token_type")]
		public abstract SupportedTokenTypes TokenType { get; }

		[JsonIgnore]
		public TimeSpan ExpiresIn { get; protected set; }

		[JsonProperty("expires_in")]
		public int ExpiresInTimeStamp => (int) this.ExpiresIn.TotalSeconds;

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; protected set; }

		[JsonIgnore]
		public bool IsExpired => DateTime.Now > this.CreatedAt.Add(this.ExpiresIn);

		#endregion
	}
}