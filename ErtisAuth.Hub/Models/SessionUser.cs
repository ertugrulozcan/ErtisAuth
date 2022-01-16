using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Hub.Models
{
    public readonly struct SessionUser
	{
		#region Properties

		public string UserId { get; }
		
		public string Username { get; }
		
		public string Email { get; }
		
		public string FirstName { get; }
		
		public string LastName { get; }
		
		public string PhotoUrl { get; }
		
		public string Role { get; }
		
		public string MembershipId { get; }
		
		public string AccessToken { get; }
		
		public string RefreshToken { get; }

		public string FullName => $"{this.FirstName} {this.LastName}";

		public string NameAbbreviation
		{
			get
			{
				string part1 = string.IsNullOrEmpty(this.FirstName) ? string.Empty : this.FirstName.ToUpper()[0].ToString();
				string part2 = string.IsNullOrEmpty(this.LastName) ? string.Empty : this.LastName.ToUpper()[0].ToString();
				return $"{part1}{part2}";
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="username"></param>
		/// <param name="email"></param>
		/// <param name="firstName"></param>
		/// <param name="lastName"></param>
		/// <param name="photoUrl"></param>
		/// <param name="role"></param>
		/// <param name="membershipId"></param>
		/// <param name="accessToken"></param>
		/// <param name="refreshToken"></param>
		public SessionUser(
			string userId,
			string username,
			string email,
			string firstName,
			string lastName,
			string photoUrl,
			string role,
			string membershipId,
			string accessToken,
			string refreshToken)
		{
			this.UserId = userId;
			this.Username = username;
			this.Email = email;
			this.FirstName = firstName;
			this.LastName = lastName;
			this.PhotoUrl = photoUrl;
			this.Role = role;
			this.MembershipId = membershipId;
			this.AccessToken = accessToken;
			this.RefreshToken = refreshToken;
		}

		#endregion

		#region Properties

		public Utilizer ToUtilizer()
		{
			return new Utilizer
			{
				Id = this.UserId,
				Username = this.Username,
				Role = this.Role,
				MembershipId = this.MembershipId,
				Type = Utilizer.UtilizerType.User,
				Token = this.AccessToken,
				TokenType = SupportedTokenTypes.Bearer
			};
		}

		#endregion
	}
}