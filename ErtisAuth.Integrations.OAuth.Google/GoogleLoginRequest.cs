using ErtisAuth.Core.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Google
{
	public class GoogleLoginRequest : IProviderLoginRequest<GoogleToken, GoogleUser>
	{
		#region Properties

		[JsonIgnore]
		public KnownProviders Provider => KnownProviders.Google;
		
		[JsonIgnore]
		public GoogleUser User { get; set; }
		
		[JsonProperty("token")]
		public GoogleToken Token { get; set; }

		[JsonProperty("appId")]
		public string AppId { get; set; }

		[JsonIgnore]
		public string UserId => this.User?.Id;
		
		[JsonIgnore]
		public string EmailAddress => this.User?.EmailAddress;
		
		[JsonIgnore]
		public string AccessToken => this.Token?.AccessToken;
		
		[JsonIgnore]
		public string AvatarUrl => this.User?.Picture;

		#endregion

		#region Methods

		public bool IsValid()
		{
			if (string.IsNullOrEmpty(this.AppId))
			{
				return false;
			}
			
			if (this.User is { } user)
			{
				if (string.IsNullOrEmpty(user.Id))
				{
					return false;
				}
			
				if (string.IsNullOrEmpty(user.FirstName))
				{
					return false;
				}
			
				if (string.IsNullOrEmpty(user.EmailAddress))
				{
					return false;
				}
			}
			else
			{
				return false;
			}

			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (this.Token == null || string.IsNullOrEmpty(this.Token.AccessToken))
			{
				return false;
			}

			return true;
		}

		public object ToUser(string membershipId, string role, string userType)
		{
			return new User
			{
				MembershipId = membershipId,
				FirstName = this.User.FirstName,
				LastName = this.User.LastName,
				Username = this.User.EmailAddress,
				EmailAddress = this.User.EmailAddress,
				Role = role,
				UserType = userType,
				SourceProvider = KnownProviders.Google.ToString(),
				ConnectedAccounts = new ProviderAccountInfo[]
				{
					new()
					{
						Provider = KnownProviders.Google.ToString(),
						UserId = this.UserId,
						Token = this.AccessToken
					}
				}
			};
		}

		#endregion
	}
}