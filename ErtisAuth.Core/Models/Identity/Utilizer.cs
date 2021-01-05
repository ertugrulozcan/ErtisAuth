using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Core.Models.Identity
{
	public struct Utilizer
	{
		#region Constants

		public const string UtilizerIdClaimName = "utilizer_id";
		public const string UtilizerTypeClaimName = "utilizer_type";
		public const string UtilizerRoleClaimName = "role";
		public const string MembershipIdClaimName = "membership_id";

		#endregion
		
		#region Properties

		public string Id { get; set; }
		
		public string Type { get; set; }
		
		public string MembershipId { get; set; }
		
		public string Role { get; set; }

		#endregion

		#region Implicit Operators

		public static implicit operator Utilizer(User user) => new Utilizer
		{
			Id = user.Id,
			Type = "user",
			MembershipId = user.MembershipId,
			Role = user.Role
		};
		
		public static implicit operator Utilizer(Application application) => new Utilizer
		{
			Id = application.Id,
			Type = "application",
			MembershipId = application.MembershipId,
			Role = application.Role
		};

		#endregion
	}
}