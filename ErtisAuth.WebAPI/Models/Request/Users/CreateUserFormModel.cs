using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Users
{
	public class CreateUserFormModel
	{
		#region Properties

		[JsonProperty("firstname")]
		public string FirstName { get; set; }
		
		[JsonProperty("lastname")]
		public string LastName { get; set; }
		
		[JsonProperty("username")]
		public string Username { get; set; }
		
		[JsonProperty("email_address")]
		public string EmailAddress { get; set; }
		
		[JsonProperty("role")]
		public string Role { get; set; }
		
		[JsonProperty("password")]
		public string Password { get; set; }

		#endregion
	}
}