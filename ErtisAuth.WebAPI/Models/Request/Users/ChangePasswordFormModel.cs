using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Users
{
	public class ChangePasswordFormModel
	{
		#region Properties

		[JsonProperty("password")]
		public string Password { get; set; }

		#endregion
	}
}