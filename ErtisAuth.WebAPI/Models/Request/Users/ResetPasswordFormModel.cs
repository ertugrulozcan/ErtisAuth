using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Users
{
	public class ResetPasswordFormModel
	{
		#region Properties

		[JsonProperty("email_address")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("server")]
		public string Server { get; set; }
		
		[JsonProperty("host")]
		public string Host { get; set; }

		#endregion

		#region Methods

		public override string ToString()
		{
			return this.EmailAddress;
		}

		#endregion
	}
}