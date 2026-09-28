using System.Net;
using ErtisAuth.IntegrationTests.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Setup;

/// <summary>
/// The one-time setup of a fresh installation, end to end: HTTP, the setup token in the database, the real services.
/// </summary>
public class SetupTests : IClassFixture<FreshErtisAuthInstance>
{
	#region Fields
	
	private readonly FreshErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	public SetupTests(FreshErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<string?> GetHealthMessageAsync()
	{
		using var response = await this._instance.CreateClient().GetAsync("/healthcheck", TestContext.Current.CancellationToken);
		var health = await ErtisAuthInstance.ReadJsonAsync(response);
		return health.TryGetProperty("message", out var message) ? message.GetString() : health.GetProperty("status").GetString();
	}
	
	private async Task<string> GetErrorCodeAsync(HttpResponseMessage response)
	{
		var error = await ErtisAuthInstance.ReadJsonAsync(response);
		return error.TryGetProperty("errorCode", out var errorCode) ? errorCode.GetString()! : throw new InvalidOperationException($"No errorCode in {error}");
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task Setup_FromAFreshInstallation_ToTheFirstLogin()
	{
		// Not set up yet
		Assert.Equal("ErtisAuth has not been set up yet", await this.GetHealthMessageAsync());
		
		// No token in the database: rejected with instructions
		using (var response = await this._instance.PostSetupAsync())
		{
			Assert.True(response.StatusCode == HttpStatusCode.Unauthorized, (await ErtisAuthInstance.ReadJsonAsync(response)).ToString());
			Assert.Equal("SetupRejected", await this.GetErrorCodeAsync(response));
		}
		
		await this._instance.InsertSetupTokenAsync();
		
		// Wrong or missing header
		using (var response = await this._instance.PostSetupAsync("wrong-token-wrong-token-wrong-token-wrong"))
		{
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}
		
		using (var response = await this._instance.PostSetupAsync(token: null))
		{
			Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		}
		
		// Set up
		string membershipId;
		using (var response = await this._instance.PostSetupAsync())
		{
			var result = await ErtisAuthInstance.ReadJsonAsync(response);
			Assert.True(response.IsSuccessStatusCode, result.ToString());
			membershipId = result.GetProperty("membership").GetProperty("_id").GetString()!;
			Assert.False(string.IsNullOrEmpty(result.GetProperty("application").GetProperty("secret").GetString()));
			Assert.False(result.GetProperty("user").TryGetProperty("password_hash", out _));
		}
		
		// The setup collection is dropped, the installation is healthy
		var collections = await (await this._instance.Database.ListCollectionNamesAsync(cancellationToken: TestContext.Current.CancellationToken)).ToListAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("setup", collections);
		Assert.Equal("Healthy", await this.GetHealthMessageAsync());
		
		// Closed after the setup, even with a valid token
		await this._instance.InsertSetupTokenAsync();
		using (var response = await this._instance.PostSetupAsync())
		{
			Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
			Assert.Equal("AlreadySetUp", await this.GetErrorCodeAsync(response));
		}
		
		// The administrator can log in
		using var login = new HttpRequestMessage(HttpMethod.Post, "/generate-token")
		{
			Content = System.Net.Http.Json.JsonContent.Create(new { username = ErtisAuthInstance.AdminUsername, password = ErtisAuthInstance.AdminPassword })
		};
		
		login.Headers.Add("Membership", membershipId);
		using var loginResponse = await this._instance.CreateClient().SendAsync(login, TestContext.Current.CancellationToken);
		Assert.True(loginResponse.IsSuccessStatusCode, (await ErtisAuthInstance.ReadJsonAsync(loginResponse)).ToString());
	}
	
	#endregion
}