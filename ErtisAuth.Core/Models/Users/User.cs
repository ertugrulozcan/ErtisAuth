using Ertis.Core.Models.Resources;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Users
{
	public class User : MembershipBoundedResource, IHasSysInfo
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
		
		[JsonProperty("password_hash")]
		public string PasswordHash { get; set; }
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}