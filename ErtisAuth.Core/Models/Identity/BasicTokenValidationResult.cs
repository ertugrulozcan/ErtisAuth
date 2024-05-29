using System.Text.Json.Serialization;
using ErtisAuth.Core.Models.Applications;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public readonly struct BasicTokenValidationResult : ITokenValidationResult
	{
		#region Properties

		[JsonProperty("verified")]
		[JsonPropertyName("verified")]
		public bool IsValidated { get; }

		[JsonProperty("token")]
		[JsonPropertyName("token")]
		public string Token { get; }
		
		[Newtonsoft.Json.JsonIgnore]
		[System.Text.Json.Serialization.JsonIgnore]
		public Application Application { get; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="isVerified"></param>
		/// <param name="token"></param>
		/// <param name="application"></param>
		public BasicTokenValidationResult(bool isVerified, string token, Application application)
		{
			this.IsValidated = isVerified;
			this.Token = token;
			this.Application = application;
		}

		#endregion
	}
}