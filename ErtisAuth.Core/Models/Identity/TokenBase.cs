using System;
using System.Linq;
using System.Text.Json.Serialization;
using ErtisAuth.Core.Exceptions;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public abstract class TokenBase
	{
		#region Properties

		[JsonProperty("access_token")]
		[JsonPropertyName("access_token")]
		public string AccessToken { get; set; }
		
		[JsonProperty("token_type")]
		[JsonPropertyName("token_type")]
		public abstract SupportedTokenTypes TokenType { get; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		protected TimeSpan ExpiresIn { get; set; }

		[JsonProperty("expires_in")]
		[JsonPropertyName("expires_in")]
		public int ExpiresInTimeStamp => (int) this.ExpiresIn.TotalSeconds;

		[JsonProperty("created_at")]
		[JsonPropertyName("created_at")]
		public DateTime CreatedAt { get; protected set; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
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
		
		public static string ExtractToken(string authorizationHeader, out string tokenType)
		{
			if (string.IsNullOrEmpty(authorizationHeader))
			{
				tokenType = null;
				return null;
			}

			var parts = authorizationHeader.Split(' ');
			if (parts.Length > 2)
			{
				throw ErtisAuthException.InvalidToken();
			}

			if (parts.Length == 2)
			{
				var supportedTokenTypes = Enum.GetValues(typeof(SupportedTokenTypes)).Cast<SupportedTokenTypes>().Select(x => x.ToString());
				if (!supportedTokenTypes.Contains(parts[0]))
				{
					throw ErtisAuthException.UnsupportedTokenType();
				}

				tokenType = parts[0];
				return parts[1];
			}
			
			tokenType = null;
			return parts[0];
		}

		#endregion
	}
}