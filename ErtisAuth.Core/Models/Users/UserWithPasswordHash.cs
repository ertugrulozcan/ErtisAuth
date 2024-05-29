using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Users
{
	public class UserWithPasswordHash : User
	{
		#region Properties

		[JsonProperty("password_hash")]
		[JsonPropertyName("password_hash")]
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
			this.Permissions = user.Permissions;
			this.Forbidden = user.Forbidden;
			this.UserType = user.UserType;
			this.SourceProvider = user.SourceProvider;
			this.ConnectedAccounts = user.ConnectedAccounts;
			this.IsActive = user.IsActive;
			this.MembershipId = user.MembershipId;
			this.Sys = user.Sys;
		}

		#endregion
	}
}