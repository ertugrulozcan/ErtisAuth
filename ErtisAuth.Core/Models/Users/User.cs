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
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}

	public class UserWithPassword : User
	{
		#region Properties

		[JsonProperty("password_hash")]
		public string PasswordHash { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Default Constructor
		/// </summary>
		public UserWithPassword()
		{
			
		}
		
		/// <summary>
		/// Constructor with user
		/// </summary>
		public UserWithPassword(User user)
		{
			this.Id = user.Id;
			this.FirstName = user.FirstName;
			this.LastName = user.LastName;
			this.Username = user.Username;
			this.EmailAddress = user.EmailAddress;
			this.Role = user.Role;
			this.Sys = user.Sys;
			this.MembershipId = user.MembershipId;
		}

		#endregion
	}
}