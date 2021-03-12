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
		protected TimeSpan ExpiresIn { get; set; }

		[JsonProperty("expires_in")]
		public int ExpiresInTimeStamp => (int) this.ExpiresIn.TotalSeconds;

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; protected set; }

		[JsonIgnore]
		public bool IsExpired => DateTime.Now > this.CreatedAt.Add(this.ExpiresIn);

		#endregion

		#region Methods

		public override string ToString()
		{
			// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
			return this.TokenType switch
			{
				SupportedTokenTypes.Basic => $"Basic {this.AccessToken}",
				SupportedTokenTypes.Bearer => $"Bearer {this.AccessToken}",
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		#endregion
	}
}