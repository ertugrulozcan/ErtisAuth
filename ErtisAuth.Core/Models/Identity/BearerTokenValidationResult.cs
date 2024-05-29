using System;
using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Users;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public readonly struct BearerTokenValidationResult : ITokenValidationResult
	{
		#region Properties

		[JsonProperty("verified")]
		[JsonPropertyName("verified")]
		public bool IsValidated { get; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public bool IsRefreshToken { get; }
		
		[JsonProperty("token")]
		[JsonPropertyName("token")]
		public string Token { get; }
		
		[JsonProperty("token_kind")]
		[JsonPropertyName("token_kind")]
		public string TokenKind
		{
			get
			{
				if (this.IsRefreshToken)
				{
					return "refresh_token";
				}

				return "access_token";
			}
		}
		
		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public User User { get; }

		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public TimeSpan RemainingTime { get; }

		[JsonProperty("remaining_time")]
		[JsonPropertyName("remaining_time")]
		public int RemainingTimeUnixEpoch => (int) this.RemainingTime.TotalSeconds;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isVerified"></param>
		/// <param name="token"></param>
		/// <param name="user"></param>
		/// <param name="remainingTime"></param>
		/// <param name="isRefreshToken"></param>
		public BearerTokenValidationResult(bool isVerified, string token, User user, TimeSpan remainingTime, bool isRefreshToken = false)
		{
			this.IsValidated = isVerified;
			this.Token = token;
			this.User = user;
			this.RemainingTime = remainingTime;
			this.IsRefreshToken = isRefreshToken;
		}

		#endregion
	}
}