namespace ErtisAuth.Integrations.OAuth.Core
{
	public interface IProviderLoginRequest
	{
		#region Properties

		KnownProviders Provider { get; }
		
		string UserId { get; }
		
		string EmailAddress { get; }
		
		string Token { get; }
		
		string AvatarUrl { get; }
		
		#endregion

		#region Methods

		bool IsValid();

		object ToUser(string membershipId, string role, string userType);

		#endregion
	}
	
	public interface IProviderLoginRequest<TUser> : IProviderLoginRequest where TUser : IProviderUser
	{
		#region Properties

		TUser User { get; set; }

		#endregion
	}
}