using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.Tokens;

/// <summary>
/// Tokens are accepted until the expiry time in the token ('exp') and rejected with TokenWasExpired /
/// RefreshTokenWasExpired right after it, in real time (the membership's tokens live a few seconds).
/// </summary>
public class TokenExpiryTests : IClassFixture<ShortLivedTokenErtisAuthInstance>
{
	#region Fields
	
	private readonly ShortLivedTokenErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public TokenExpiryTests(ShortLivedTokenErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	/// <summary>
	/// The 'exp' claim of the JWT.
	/// </summary>
	private static DateTimeOffset ReadExpiry(string token)
	{
		var payload = token.Split('.')[1].Replace('-', '+').Replace('_', '/');
		payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
		var claims = JsonNode.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)))!;
		return DateTimeOffset.FromUnixTimeSeconds(claims["exp"]!.GetValue<long>());
	}
	
	private static async Task WaitUntilAsync(DateTimeOffset time)
	{
		var delay = time - DateTimeOffset.UtcNow;
		if (delay > TimeSpan.Zero)
		{
			await Task.Delay(delay, CancellationToken);
		}
	}
	
	private async Task<HttpResponseMessage> MeAsync(string accessToken) =>
		await this._instance.CreateClient($"Bearer {accessToken}").GetAsync("/me", CancellationToken);
	
	private async Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
		await this._instance.CreateClient($"Bearer {refreshToken}").GetAsync("/refresh-token", CancellationToken);
	
	private static async Task AssertErrorAsync(HttpResponseMessage response, string errorCode)
	{
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Unauthorized);
		Assert.Equal(errorCode, error!["errorCode"]!.GetValue<string>());
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task AccessToken_IsAcceptedUntilItsExpiryAndRejectedRightAfter()
	{
		var generatedAt = DateTimeOffset.UtcNow;
		var (accessToken, _) = await this._instance.GenerateTokenAsync();
		var expiry = ReadExpiry(accessToken);
		
		// 'exp' is the membership's expires_in after the generation (whole seconds)
		Assert.InRange(expiry, generatedAt.AddSeconds(ShortLivedTokenErtisAuthInstance.ExpiresIn - 2), DateTimeOffset.UtcNow.AddSeconds(ShortLivedTokenErtisAuthInstance.ExpiresIn + 1));
		
		await WaitUntilAsync(expiry.AddMilliseconds(-1000));
		using (var beforeExpiry = await this.MeAsync(accessToken))
		{
			await ResourceClient.AssertStatusAsync(beforeExpiry, HttpStatusCode.OK);
		}
		
		await WaitUntilAsync(expiry.AddMilliseconds(300));
		using (var afterExpiry = await this.MeAsync(accessToken))
		{
			await AssertErrorAsync(afterExpiry, "TokenWasExpired");
		}
		
		using (var verifyResponse = await this._instance.CreateClient($"Bearer {accessToken}").GetAsync("/verify-token", CancellationToken))
		{
			await AssertErrorAsync(verifyResponse, "TokenWasExpired");
		}
	}
	
	[Fact]
	public async Task RefreshToken_OutlivesTheAccessTokenAndIsRejectedRightAfterItsExpiry()
	{
		var (accessToken, refreshToken) = await this._instance.GenerateTokenAsync();
		var accessTokenExpiry = ReadExpiry(accessToken);
		var refreshTokenExpiry = ReadExpiry(refreshToken);
		Assert.Equal(ShortLivedTokenErtisAuthInstance.RefreshTokenExpiresIn - ShortLivedTokenErtisAuthInstance.ExpiresIn, (refreshTokenExpiry - accessTokenExpiry).TotalSeconds, 1.0);
		
		// The access token has expired, the refresh token still works
		await WaitUntilAsync(accessTokenExpiry.AddMilliseconds(300));
		using (var expiredAccess = await this.MeAsync(accessToken))
		{
			await AssertErrorAsync(expiredAccess, "TokenWasExpired");
		}
		
		using (var refreshResponse = await this.RefreshAsync(refreshToken))
		{
			var refreshed = await ResourceClient.AssertStatusAsync(refreshResponse, HttpStatusCode.Created);
			using var newAccess = await this.MeAsync(refreshed!["access_token"]!.GetValue<string>());
			await ResourceClient.AssertStatusAsync(newAccess, HttpStatusCode.OK);
		}
		
		// A second pair, to try its refresh token after its expiry (the first one was revoked by the refresh)
		var (_, secondRefreshToken) = await this._instance.GenerateTokenAsync();
		await WaitUntilAsync(ReadExpiry(secondRefreshToken).AddMilliseconds(300));
		using (var expiredRefresh = await this.RefreshAsync(secondRefreshToken))
		{
			await AssertErrorAsync(expiredRefresh, "RefreshTokenWasExpired");
		}
	}
	
	#endregion
}