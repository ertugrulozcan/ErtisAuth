using System;
using System.Linq;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Core.Models.Identity
{
	public struct Utilizer
	{
		#region Constants

		public const string UtilizerIdClaimName = "utilizer_id";
		public const string UtilizerTypeClaimName = "utilizer_type";
		public const string UtilizerUsernameClaimName = "utilizer_username";
		public const string UtilizerRoleClaimName = "role";
		public const string MembershipIdClaimName = "membership_id";
		public const string UtilizerTokenClaimName = "access_token";
		public const string UtilizerTokenTypeClaimName = "token_type";
		
		#endregion
		
		#region Properties

		public string Id { get; set; }
		
		public UtilizerType Type { get; set; }
		
		public string Username { get; set; }
		
		public string MembershipId { get; set; }
		
		public string Role { get; set; }
		
		public string Token { get; set; }
		
		public SupportedTokenTypes TokenType { get; set; }

		#endregion

		#region Implicit Operators

		public static implicit operator Utilizer(User user) => new Utilizer
		{
			Id = user.Id,
			Type = UtilizerType.User,
			MembershipId = user.MembershipId,
			Role = user.Role
		};
		
		public static implicit operator Utilizer(Application application) => new Utilizer
		{
			Id = application.Id,
			Type = UtilizerType.Application,
			MembershipId = application.MembershipId,
			Role = application.Role
		};

		#endregion

		#region Methods

		public static UtilizerType ParseType(string type)
		{
			if (string.IsNullOrEmpty(type) || string.IsNullOrWhiteSpace(type))
			{
				return UtilizerType.None;
			}
			
			type = type.ToLower();
			type = char.ToUpper(type[0]) + type.Substring(1);
			
			if (Enum.GetNames(typeof(UtilizerType)).Any(x => x == type))
			{
				return (UtilizerType) Enum.Parse(typeof(UtilizerType), type);
			}

			return UtilizerType.None;
		}

		#endregion

		#region Enums

		public enum UtilizerType
		{
			None,
			System,
			User,
			Application
		}

		#endregion
	}
}