namespace ErtisAuth.Integrations.OAuth.Core
{
	public interface IProviderLoginRequest
	{
		#region Properties

		KnownProviders Provider { get; }
		
		string UserId { get; }
		
		string EmailAddress { get; }
		
		string AvatarUrl { get; }
		
		string AccessToken { get; }
		
		#endregion

		#region Methods

		bool IsValid();

		object ToUser(string membershipId, string role, string userType);

		#endregion
	}
	
	public interface IProviderLoginRequest<TToken, TUser> : IProviderLoginRequest where TToken : IProviderToken where TUser : IProviderUser
	{
		#region Properties

		TToken Token { get; set; }
		
		TUser User { get; set; }

		#endregion
	}
}