using ErtisAuth.Core.Models.Users;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Memberships
{
	public class UpdateMembershipFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("expires_in")]
		public int ExpiresIn { get; set; }
		
		[JsonProperty("refresh_token_expires_in")]
		public int RefreshTokenExpiresIn { get; set; }
		
		[JsonProperty("user_type")]
		public UserType UserType { get; set; }
		
		#endregion
	}
}