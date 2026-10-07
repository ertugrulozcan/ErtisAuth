using System.Net;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.Sdk.Extensions;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.IntegrationTests.Sdk;

/// <summary>
/// The SDK's active / revoked token queries of a user against the real ErtisAuth
/// (regression: the queries were built with single quotes, which the System.Text.Json based API rejects with 400).
/// </summary>
public class SdkUserTokensTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public SdkUserTokensTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private IUserService CreateUserService()
	{
		var services = new ServiceCollection();
		services.AddErtisAuth(options =>
		{
			options.BaseUrl = this._instance.CreateClient().BaseAddress!.ToString().TrimEnd('/');
			options.MembershipId = this._instance.MembershipId;
		});
		
		return services.BuildServiceProvider().GetRequiredService<IUserService>();
	}
	
	#endregion
	
	#region Methods
	
	[Fact]
	public async Task GetActiveTokensAsync_ReturnsTheActiveTokensOfTheUser()
	{
		var (accessToken, _) = await this._instance.GenerateTokenAsync();
		var adminUserId = await this._instance.GetAdminUserIdAsync();
		
		var response = await this.CreateUserService().GetActiveTokensAsync(adminUserId, BearerToken.CreateTemp(accessToken), cancellationToken: CancellationToken);
		
		Assert.True(response.IsSuccess, $"{response.StatusCode}: {response.Message}");
		Assert.Contains(response.Data!.Items, x => x.AccessToken == accessToken && x.UserId == adminUserId);
	}
	
	[Fact]
	public async Task GetRevokedTokensAsync_ReturnsTheRevokedTokensOfTheUser()
	{
		var (revokedAccessToken, _) = await this._instance.GenerateTokenAsync();
		using var revokeResponse = await this._instance.CreateClient($"Bearer {revokedAccessToken}").GetAsync("/revoke-token", CancellationToken);
		Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
		
		var (accessToken, _) = await this._instance.GenerateTokenAsync();
		var adminUserId = await this._instance.GetAdminUserIdAsync();
		
		var response = await this.CreateUserService().GetRevokedTokensAsync(adminUserId, BearerToken.CreateTemp(accessToken), cancellationToken: CancellationToken);
		
		Assert.True(response.IsSuccess, $"{response.StatusCode}: {response.Message}");
		Assert.Contains(response.Data!.Items, x => x.Token == revokedAccessToken);
	}
	
	#endregion
}
