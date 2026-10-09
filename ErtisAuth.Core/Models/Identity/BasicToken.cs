using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ErtisAuth.Core.Models.Identity
{
	public class BasicToken : TokenBase
	{
		#region Properties

		[JsonConverter(typeof(StringEnumConverter))]
		[JsonProperty("token_type")]
		public override SupportedTokenTypes TokenType => SupportedTokenTypes.Basic;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="token"></param>
		public BasicToken(string token)
		{
			this.AccessToken = token;
			this.ExpiresIn = TimeSpan.MaxValue;
			this.CreatedAt = DateTime.Now;
		}

		#endregion
	}
}