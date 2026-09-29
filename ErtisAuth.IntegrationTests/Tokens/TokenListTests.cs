using System.Net;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;

namespace ErtisAuth.IntegrationTests.Tokens;

/// <summary>
/// The active and revoked token lists of a membership as login, refresh and revoke (logout) change them.
/// </summary>
public class TokenListTests : IClassFixture<ErtisAuthInstance>
{
	#region Constants
	
	private const string Password = "Session-P@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	public TokenListTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<ResourceClient> AdminResourceClientAsync(string resource) =>
		new(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/{resource}");
	
	private async Task<(string Id, string Username)> CreateUserAsync(string role = "admin")
	{
		var username = $"session{Guid.NewGuid():N}";
		var users = await this.AdminResourceClientAsync("users");
		var user = await users.CreateAsync(new
		{
			username,
			firstname = "Jane",
			lastname = "Doe",
			email_address = $"{username}@example.com",
			password = Password,
			role,
			user_type = "user"
		});
		
		return (user["_id"]!.GetValue<string>(), username);
	}
	
	private async Task<JsonObject[]> ActiveTokensOfAsync(string userId)
	{
		var activeTokens = await this.AdminResourceClientAsync("active-tokens");
		var items = await activeTokens.ListAsync("?limit=500");
		return items.Select(x => x!.AsObject()).Where(x => x["user_id"]?.GetValue<string>() == userId).ToArray();
	}
	
	private async Task<JsonObject[]> RevokedTokensOfAsync(string userId)
	{
		var revokedTokens = await this.AdminResourceClientAsync("revoked-tokens");
		var items = await revokedTokens.ListAsync("?limit=500");
		return items.Select(x => x!.AsObject()).Where(x => x["user_id"]?.GetValue<string>() == userId).ToArray();
	}
	
	private async Task RevokeAsync(string accessToken, bool logoutFromAllDevices = false)
	{
		var url = logoutFromAllDevices ? "/revoke-token?logout-all=true" : "/revoke-token";
		using var response = await this._instance.CreateClient($"Bearer {accessToken}").GetAsync(url, CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NoContent);
	}
	
	private async Task AssertRejectedAsync(string accessToken)
	{
		using var response = await this._instance.CreateClient($"Bearer {accessToken}").GetAsync("/me", CancellationToken);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Login_AddsAnActiveToken()
	{
		var (userId, username) = await this.CreateUserAsync();
		var before = DateTime.UtcNow;
		var (accessToken, refreshToken) = await this._instance.GenerateTokenAsync(username, Password);
		
		var activeToken = Assert.Single(await this.ActiveTokensOfAsync(userId));
		Assert.Equal(accessToken, activeToken["access_token"]!.GetValue<string>());
		Assert.Equal(refreshToken, activeToken["refresh_token"]!.GetValue<string>());
		Assert.Equal(username, activeToken["username"]!.GetValue<string>());
		
		// Expires after the membership's expires_in (3600 s); kept at least until then
		var expireTime = activeToken["expire_time"]!.GetValue<DateTime>().ToUniversalTime();
		Assert.InRange(expireTime, before.AddSeconds(3600 - 5), DateTime.UtcNow.AddSeconds(3600 + 5));
		Assert.True(activeToken["retain_until"]!.GetValue<DateTime>().ToUniversalTime() >= expireTime);
	}
	
	[Fact]
	public async Task Revoke_MovesTheTokenToTheRevokedTokens()
	{
		var (userId, username) = await this.CreateUserAsync();
		var (accessToken, refreshToken) = await this._instance.GenerateTokenAsync(username, Password);
		
		await this.RevokeAsync(accessToken);
		
		Assert.Empty(await this.ActiveTokensOfAsync(userId));
		var revoked = await this.RevokedTokensOfAsync(userId);
		var revokedTokens = revoked.Select(x => x["token"]!.GetValue<string>()).ToArray();
		Assert.Contains(accessToken, revokedTokens);
		Assert.Contains(refreshToken, revokedTokens);
		Assert.All(revoked, x => Assert.NotNull(x["revoked_at"]));
		Assert.All(revoked, x => Assert.NotNull(x["retain_until"]));
		await this.AssertRejectedAsync(accessToken);
	}
	
	[Fact]
	public async Task RevokeWithLogoutAll_RevokesEverySessionOfTheUser()
	{
		var (userId, username) = await this.CreateUserAsync();
		var first = await this._instance.GenerateTokenAsync(username, Password);
		var second = await this._instance.GenerateTokenAsync(username, Password);
		Assert.Equal(2, (await this.ActiveTokensOfAsync(userId)).Length);
		
		await this.RevokeAsync(first.AccessToken, logoutFromAllDevices: true);
		
		Assert.Empty(await this.ActiveTokensOfAsync(userId));
		await this.AssertRejectedAsync(first.AccessToken);
		await this.AssertRejectedAsync(second.AccessToken);
	}
	
	[Fact]
	public async Task Revoke_KeepsTheOtherSessionsOfTheUser()
	{
		var (userId, username) = await this.CreateUserAsync();
		var first = await this._instance.GenerateTokenAsync(username, Password);
		var second = await this._instance.GenerateTokenAsync(username, Password);
		
		await this.RevokeAsync(first.AccessToken);
		
		var remaining = Assert.Single(await this.ActiveTokensOfAsync(userId));
		Assert.Equal(second.AccessToken, remaining["access_token"]!.GetValue<string>());
		using var meResponse = await this._instance.CreateClient($"Bearer {second.AccessToken}").GetAsync("/me", CancellationToken);
		await ResourceClient.AssertStatusAsync(meResponse, HttpStatusCode.OK);
	}
	
	[Fact]
	public async Task Refresh_ReplacesTheActiveToken()
	{
		var (userId, username) = await this.CreateUserAsync();
		var (accessToken, refreshToken) = await this._instance.GenerateTokenAsync(username, Password);
		
		using var refreshResponse = await this._instance.CreateClient($"Bearer {refreshToken}").GetAsync("/refresh-token", CancellationToken);
		var refreshed = await ResourceClient.AssertStatusAsync(refreshResponse, HttpStatusCode.Created);
		var newAccessToken = refreshed!["access_token"]!.GetValue<string>();
		
		var activeToken = Assert.Single(await this.ActiveTokensOfAsync(userId));
		Assert.Equal(newAccessToken, activeToken["access_token"]!.GetValue<string>());
		Assert.Contains(accessToken, (await this.RevokedTokensOfAsync(userId)).Select(x => x["token"]!.GetValue<string>()));
	}
	
	[Fact]
	public async Task TokenLists_ContainOnlyTheTokensOfTheMembership()
	{
		var otherUserId = ObjectId.GenerateNewId().ToString();
		await this._instance.Database.GetCollection<BsonDocument>("active_tokens").InsertOneAsync(new BsonDocument
		{
			{ "access_token", "other-access-token" },
			{ "user_id", otherUserId },
			{ "membership_id", "other-membership" }
		}, cancellationToken: CancellationToken);
		
		await this._instance.Database.GetCollection<BsonDocument>("revoked_tokens").InsertOneAsync(new BsonDocument
		{
			{ "token", "other-revoked-token" },
			{ "user_id", otherUserId },
			{ "membership_id", "other-membership" }
		}, cancellationToken: CancellationToken);
		
		Assert.Empty(await this.ActiveTokensOfAsync(otherUserId));
		Assert.Empty(await this.RevokedTokensOfAsync(otherUserId));
	}
	
	[Fact]
	public async Task TokenLists_RequireTheirPermissions()
	{
		var roles = await this.AdminResourceClientAsync("roles");
		var role = await roles.CreateAsync(new { name = $"No Tokens {Guid.NewGuid():N}", permissions = new[] { "users.read" } });
		var (_, username) = await this.CreateUserAsync(role["slug"]!.GetValue<string>());
		var (accessToken, _) = await this._instance.GenerateTokenAsync(username, Password);
		var client = this._instance.CreateClient($"Bearer {accessToken}");
		
		using var activeResponse = await client.GetAsync($"{this.MembershipUrl}/active-tokens", CancellationToken);
		Assert.Equal(HttpStatusCode.Forbidden, activeResponse.StatusCode);
		using var revokedResponse = await client.GetAsync($"{this.MembershipUrl}/revoked-tokens", CancellationToken);
		Assert.Equal(HttpStatusCode.Forbidden, revokedResponse.StatusCode);
	}
	
	#endregion
}