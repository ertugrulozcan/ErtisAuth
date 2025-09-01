using ErtisAuth.Core.Models.Users;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Facebook
{
	public class FacebookLoginRequest : IProviderLoginRequest<FacebookUserToken, FacebookUserToken>
	{
		#region Properties

		[JsonIgnore]
		public KnownProviders Provider => KnownProviders.Facebook;
		
		[JsonProperty("user")]
		public FacebookUserToken User { get; set; }
		
		[JsonProperty("appId")]
		public string AppId { get; set; }
		
		[JsonIgnore]
		public FacebookUserToken Token { get; set; }
		
		[JsonIgnore]
		public string AccessToken => this.User?.AccessToken ?? this.Token?.AccessToken;
		
		[JsonIgnore]
		public string UserId => this.User?.Id;
		
		[JsonIgnore]
		public string EmailAddress => this.User?.EmailAddress;
		
		[JsonIgnore]
		public string AvatarUrl => this.User?.Picture?.Data?.Url;
		
		[JsonIgnore]
		public bool IsLimited { get; set; }

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
				
				if (string.IsNullOrEmpty(user.AccessToken))
				{
					return false;
				}
			}
			else
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
				SourceProvider = KnownProviders.Facebook.ToString(),
				ConnectedAccounts = new ProviderAccountInfo[]
				{
					new()
					{
						Provider = KnownProviders.Facebook.ToString(),
						UserId = this.UserId,
						Token = this.AccessToken
					}
				}
			};
		}

		#endregion
	}
}