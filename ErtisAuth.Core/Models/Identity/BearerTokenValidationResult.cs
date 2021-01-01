using System;
using ErtisAuth.Core.Models.Users;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public readonly struct BearerTokenValidationResult
	{
		#region Properties

		[JsonProperty("verified")]
		public bool IsValidated { get; }

		[JsonIgnore]
		public bool IsRefreshToken { get; }
		
		[JsonProperty("token")]
		public string Token { get; }
		
		[JsonProperty("token_type")]
		public string TokenType
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
		
		[JsonIgnore]
		public User User { get; }

		[JsonIgnore]
		public TimeSpan RemainingTime { get; }

		[JsonProperty("remaining_time")]
		public int RemainingTimeUnixEpoch
		{
			get
			{
				return (int) this.RemainingTime.TotalSeconds;
			}
		}
		
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