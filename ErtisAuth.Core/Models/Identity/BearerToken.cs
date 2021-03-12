using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Core.Models.Identity
{
	public class BearerToken : TokenBase, IRefreshableToken
	{
		#region Properties

		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty("token_type")]
		public override SupportedTokenTypes TokenType => SupportedTokenTypes.Bearer;

		[JsonProperty("refresh_token")]
		public string RefreshToken { get; }

		[JsonIgnore]
		public TimeSpan RefreshExpiresIn { get; private set; }
		
		[JsonProperty("refresh_token_expires_in")]
		public int RefreshTokenExpiresInTimeStamp => (int) this.RefreshExpiresIn.TotalSeconds;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Private Constructor
		/// </summary>
		private BearerToken()
		{
			
		}
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="token"></param>
		/// <param name="expiresIn"></param>
		/// <param name="refreshToken"></param>
		/// <param name="refreshExpiresIn"></param>
		public BearerToken(string token, TimeSpan expiresIn, string refreshToken, TimeSpan refreshExpiresIn)
		{
			this.AccessToken = token;
			this.ExpiresIn = expiresIn;
			this.RefreshToken = refreshToken;
			this.RefreshExpiresIn = refreshExpiresIn;
			this.CreatedAt = DateTime.Now;
		}

		#endregion

		#region Methods

		public static BearerToken CreateTemp(string token)
		{
			return new BearerToken
			{
				AccessToken = token,
				CreatedAt = DateTime.Now
			};
		}

		#endregion
	}
}