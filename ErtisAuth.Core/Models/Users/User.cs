using System.Collections.Generic;
using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Identity;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Users
{
	public class User : MembershipBoundedResource, IUtilizer, IHasSysInfo
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
		
		[JsonProperty("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[JsonProperty("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }

		[JsonProperty("sys")]
		public SysModel Sys { get; set; }

		[JsonIgnore] 
		public Utilizer.UtilizerType UtilizerType => Utilizer.UtilizerType.User;
		
		#endregion
	}
}