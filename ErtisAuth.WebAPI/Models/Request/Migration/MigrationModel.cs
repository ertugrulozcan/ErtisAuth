using ErtisAuth.Core.Models.Users;
using ErtisAuth.WebAPI.Models.Request.Applications;
using ErtisAuth.WebAPI.Models.Request.Memberships;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Migration
{
	public class MigrationModel
	{
		#region Properties

		[JsonProperty("membership")]
		public CreateMembershipFormModel Membership { get; set; }
		
		[JsonProperty("user")]
		public UserWithPassword User { get; set; }
		
		[JsonProperty("application")]
		public CreateApplicationFormModel Application { get; set; }

		#endregion
	}
}