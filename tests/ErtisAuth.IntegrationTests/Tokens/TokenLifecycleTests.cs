using System.Net;
using ErtisAuth.IntegrationTests.Authorization;
using ErtisAuth.IntegrationTests.Infrastructure;

namespace ErtisAuth.IntegrationTests.Tokens;

/// <summary>
/// Token lifecycle over HTTP with the real token store: generate, use, refresh, revoke; and Basic authentication
/// with an application secret.
/// </summary>
public class TokenLifecycleTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public TokenLifecycleTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<HttpResponseMessage> GetAsync(string url, string authorization)
	{
		return await this._instance.CreateClient(authorization).GetAsync(url, TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Bearer
	
	[Fact]
	public async Task Me_WithAGeneratedToken_ReturnsTheAdministrator()
	{
		var (accessToken, _) = await this._instance.GenerateTokenAsync();
		
		using var response = await this.GetAsync("/me", $"Bearer {accessToken}");
		var me = await ErtisAuthInstance.ReadJsonAsync(response);
		
		Assert.True(response.IsSuccessStatusCode, me.ToString());
		Assert.Equal(ErtisAuthInstance.AdminUsername, me.GetProperty("username").GetString());
		Assert.False(me.TryGetProperty("password_hash", out _));
	}
	
	/// <summary>
	/// The authorization scheme is case-insensitive (RFC 7235)
	/// </summary>
	[Theory]
	[InlineData("bearer")]
	[InlineData("BEARER")]
	public async Task Me_WithTheSchemeInAnotherCase_ReturnsTheAdministrator(string scheme)
	{
		var (accessToken, _) = await this._instance.GenerateTokenAsync();
		
		using var response = await this.GetAsync("/me", $"{scheme} {accessToken}");
		
		Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
	}
	
	[Fact]
	public async Task GenerateToken_WithWrongPassword_IsRejected()
	{
		await Assert.ThrowsAsync<InvalidOperationException>(() => this._instance.GenerateTokenAsync(password: "wrong-password"));
	}
	
	[Fact]
	public async Task RefreshToken_ReturnsAUsableTokenAndRevokesTheOldPair()
	{
		var (accessToken, refreshToken) = await this._instance.GenerateTokenAsync();
		
		using var refreshResponse = await this.GetAsync("/refresh-token", $"Bearer {refreshToken}");
		var refreshed = await ErtisAuthInstance.ReadJsonAsync(refreshResponse);
		Assert.True(refreshResponse.IsSuccessStatusCode, refreshed.ToString());
		
		using var newTokenResponse = await this.GetAsync("/me", $"Bearer {refreshed.GetProperty("access_token").GetString()}");
		Assert.True(newTokenResponse.IsSuccessStatusCode);
		
		// Refreshing revokes the previous pair (replay protection)
		using var oldTokenResponse = await this.GetAsync("/me", $"Bearer {accessToken}");
		Assert.Equal(HttpStatusCode.Unauthorized, oldTokenResponse.StatusCode);
		using var replayResponse = await this.GetAsync("/refresh-token", $"Bearer {refreshToken}");
		Assert.Equal(HttpStatusCode.Unauthorized, replayResponse.StatusCode);
	}
	
	[Fact]
	public async Task Me_WithARefreshToken_IsRejected()
	{
		var (_, refreshToken) = await this._instance.GenerateTokenAsync();
		
		using var response = await this.GetAsync("/me", $"Bearer {refreshToken}");
		
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		MembershipIsolationTests.AssertWwwAuthenticate(response);
	}
	
	[Fact]
	public async Task RevokeToken_MakesTheAccessAndRefreshTokensUnusable()
	{
		var (accessToken, refreshToken) = await this._instance.GenerateTokenAsync();
		
		using var revokeResponse = await this.GetAsync("/revoke-token", $"Bearer {accessToken}");
		Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);
		
		using var meResponse = await this.GetAsync("/me", $"Bearer {accessToken}");
		Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
		using var refreshResponse = await this.GetAsync("/refresh-token", $"Bearer {refreshToken}");
		Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
	}
	
	#endregion
	
	#region Basic
	
	[Fact]
	public async Task WhoAmI_WithTheApplicationSecret_ReturnsTheApplication()
	{
		using var response = await this.GetAsync("/whoami", $"Basic {this._instance.ApplicationId}:{this._instance.ApplicationSecret}");
		var application = await ErtisAuthInstance.ReadJsonAsync(response);
		
		Assert.True(response.IsSuccessStatusCode, application.ToString());
		Assert.Equal(this._instance.ApplicationId, application.GetProperty("_id").GetString());
	}
	
	[Fact]
	public async Task WhoAmI_WithTheSchemeInLowercase_ReturnsTheApplication()
	{
		using var response = await this.GetAsync("/whoami", $"basic {this._instance.ApplicationId}:{this._instance.ApplicationSecret}");
		
		Assert.True(response.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
	}
	
	[Fact]
	public async Task WhoAmI_WithAWrongApplicationSecret_IsRejected()
	{
		using var response = await this.GetAsync("/whoami", $"Basic {this._instance.ApplicationId}:wrong-secret");
		
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		MembershipIsolationTests.AssertWwwAuthenticate(response);
	}
	
	#endregion
}