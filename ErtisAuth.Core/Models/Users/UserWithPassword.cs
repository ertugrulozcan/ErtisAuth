using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Users
{
	public class UserWithPassword : User
	{
		#region Properties
		
		[JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
		public string Password { get; set; }
		
		#endregion
	}
}