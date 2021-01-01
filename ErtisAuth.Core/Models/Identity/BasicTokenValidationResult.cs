using ErtisAuth.Core.Models.Applications;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public readonly struct BasicTokenValidationResult
	{
		#region Properties

		[JsonProperty("verified")]
		public bool IsValidated { get; }

		[JsonProperty("token")]
		public string Token { get; }
		
		[JsonIgnore]
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