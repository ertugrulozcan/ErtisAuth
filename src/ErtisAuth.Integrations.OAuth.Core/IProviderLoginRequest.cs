// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Integrations.OAuth.Core;

public interface IProviderLoginRequest
{
	#region Properties
	
	ProviderType Provider { get; }
	
	string? UserId { get; }
	
	string? EmailAddress { get; }
	
	/// <summary>
	/// True only when the provider itself asserted (in verified data) that the email address is verified.
	/// </summary>
	bool IsEmailVerified { get; }
	
	string? AvatarUrl { get; }
	
	string? AccessToken { get; }
	
	#endregion
	
	#region Methods
	
	bool IsValid();
	
	object ToUser(string membershipId, string? role, string? userType);
	
	#endregion
}

public interface IProviderLoginRequest<TToken, TUser> : IProviderLoginRequest where TToken : IProviderToken where TUser : IProviderUser
{
	#region Properties
	
	TUser? User { get; set; }
	
	TToken? Token { get; set; }
	
	#endregion
}