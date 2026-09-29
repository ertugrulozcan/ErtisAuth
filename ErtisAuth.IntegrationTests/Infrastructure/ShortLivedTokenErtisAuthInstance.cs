namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// An installation whose tokens expire within seconds (expires_in / refresh_token_expires_in of the membership),
/// to test expiry in real time.
/// </summary>
public sealed class ShortLivedTokenErtisAuthInstance : ErtisAuthInstance
{
	#region Constants
	
	public const int ExpiresIn = 4;
	
	public const int RefreshTokenExpiresIn = 8;
	
	#endregion
	
	#region Constructors
	
	public ShortLivedTokenErtisAuthInstance(MongoDbContainerFixture mongo) : base(mongo)
	{
	
	}
	
	#endregion
	
	#region Lifetime
	
	protected override async Task OnSetUpAsync()
	{
		await this.UpdateMembershipAsync(membership =>
		{
			membership["expires_in"] = ExpiresIn;
			membership["refresh_token_expires_in"] = RefreshTokenExpiresIn;
		});
	}
	
	#endregion
}