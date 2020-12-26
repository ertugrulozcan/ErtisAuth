using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Users
{
	public class UpdateUserFormModel
	{
		#region Properties

		[JsonProperty("firstname")]
		public string FirstName { get; set; }
		
		[JsonProperty("lastname")]
		public string LastName { get; set; }
		
		[JsonProperty("role")]
		public string Role { get; set; }
		
		#endregion
	}
}