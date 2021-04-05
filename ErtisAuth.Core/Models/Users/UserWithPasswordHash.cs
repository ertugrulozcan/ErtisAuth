using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Users
{
	public class UserWithPasswordHash : User
	{
		#region Properties

		[JsonProperty("password_hash")]
		public string PasswordHash { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Default Constructor
		/// </summary>
		public UserWithPasswordHash()
		{
			
		}
		
		/// <summary>
		/// Constructor with user
		/// </summary>
		public UserWithPasswordHash(User user)
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