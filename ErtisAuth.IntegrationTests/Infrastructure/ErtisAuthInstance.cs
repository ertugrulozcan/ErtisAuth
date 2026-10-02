using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

// ReSharper disable MemberCanBePrivate.Global
namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// An ErtisAuth installation for one test class: its own database in the shared container, set up through the real
/// setup flow (setup token in the "setup" collection + POST /setup). The database is dropped at the end.
/// </summary>
public class ErtisAuthInstance : IAsyncLifetime
{
	#region Constants
	
	public const string AdminUsername = "admin";
	public const string AdminPassword = "Adm1n-P@ssw0rd!";
	public const string AdminEmailAddress = "admin@example.com";
	public const string SetupToken = "3f9c2a7e1b8d4c6f0a5e9b2d7c1f4a8e6b3d0c9f2a7e5b1d8c4f6a0e3b9d2c7f";
	
	#endregion
	
	#region Fields
	
	private Uri? _serverAddress;
	
	#endregion
	
	#region Properties
	
	public ErtisAuthFactory Factory { get; }
	
	public IMongoDatabase Database { get; }
	
	private string DatabaseName { get; }
	
	/// <summary>
	/// Set up the installation when the fixture starts.
	/// </summary>
	protected virtual bool SetUpOnStart => true;
	
	public string MembershipId { get; private set; } = string.Empty;
	
	public string ApplicationId { get; private set; } = string.Empty;
	
	public string ApplicationSecret { get; private set; } = string.Empty;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mongo"></param>
	// ReSharper disable once MemberCanBeProtected.Global
	public ErtisAuthInstance(MongoDbContainerFixture mongo)
	{
		this.DatabaseName = $"ertisauth-{Guid.NewGuid():N}";
		// The callback runs when the host is built (first request), after the derived instance is constructed
		this.Factory = new ErtisAuthFactory(mongo.ConnectionString, this.DatabaseName, this.ConfigureTestServices);
		
		// Real Kestrel on a random port (no conflict with a local ErtisAuth on 9716), as in production;
		// the in-memory TestServer behaves differently in places (e.g. re-runs Response.OnStarting callbacks)
		this.Factory.UseKestrel(0);
		this.Database = new MongoClient(mongo.ConnectionString).GetDatabase(this.DatabaseName);
	}
	
	#endregion
	
	#region Lifetime
	
	public async ValueTask InitializeAsync()
	{
		if (!this.SetUpOnStart)
		{
			return;
		}
		
		await this.InsertSetupTokenAsync();
		using var response = await this.PostSetupAsync();
		var result = await ReadJsonAsync(response);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"Setup failed ({(int) response.StatusCode}): {result}");
		}
		
		this.MembershipId = result.GetProperty("membership").GetProperty("_id").GetString()!;
		this.ApplicationId = result.GetProperty("application").GetProperty("_id").GetString()!;
		this.ApplicationSecret = result.GetProperty("application").GetProperty("secret").GetString()!;
		
		await this.OnSetUpAsync();
	}
	
	/// <summary>
	/// Replaces services of the app for this installation (e.g. outgoing HTTP calls to external providers).
	/// </summary>
	protected virtual void ConfigureTestServices(IServiceCollection services)
	{
	
	}
	
	/// <summary>
	/// Runs once after the setup, to prepare the installation further (e.g. mail providers).
	/// </summary>
	protected virtual Task OnSetUpAsync() => Task.CompletedTask;
	
	public virtual async ValueTask DisposeAsync()
	{
		await this.Factory.DisposeAsync();
		await this.Database.Client.DropDatabaseAsync(this.DatabaseName);
		GC.SuppressFinalize(this);
	}
	
	#endregion
	
	#region Setup
	
	// ReSharper disable once MemberCanBePrivate.Global
	public static object SetupRequest => new
	{
		membership = new
		{
			name = "ErtisAuth Integration",
			expires_in = 3600,
			refresh_token_expires_in = 86400,
			hash_algorithm = "ARGON2ID",
			encoding = "UTF-8"
		},
		user = new
		{
			username = AdminUsername,
			firstname = "Admin",
			lastname = "User",
			email_address = AdminEmailAddress,
			password = AdminPassword,
			user_type = "User"
		},
		application = new
		{
			name = "Server",
			role = "admin"
		}
	};
	
	public async Task InsertSetupTokenAsync(string token = SetupToken)
	{
		await this.Database.GetCollection<BsonDocument>("setup").InsertOneAsync(new BsonDocument("token", token));
	}
	
	/// <param name="token"></param>
	/// <param name="body">Another setup request than <see cref="SetupRequest"/></param>
	public async Task<HttpResponseMessage> PostSetupAsync(string? token = SetupToken, object? body = null)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/setup");
		request.Content = JsonContent.Create(body ?? SetupRequest);
		
		if (token != null)
		{
			request.Headers.Add("X-Setup-Token", token);
		}
		
		return await this.CreateClient().SendAsync(request);
	}
	
	#endregion
	
	#region Clients
	
	public HttpClient CreateClient()
	{
		// Kestrel's address (random port); the options' default BaseAddress would be http://localhost
		this._serverAddress ??= this.Factory.CreateClient().BaseAddress;
		return this.Factory.CreateClient(new WebApplicationFactoryClientOptions
		{
			BaseAddress = this._serverAddress!,
			AllowAutoRedirect = false
		});
	}
	
	public HttpClient CreateClient(string authorization)
	{
		var client = this.CreateClient();
		client.DefaultRequestHeaders.Authorization = AuthenticationHeaderValue.Parse(authorization);
		return client;
	}
	
	#endregion
	
	#region Tokens
	
	public async Task<HttpResponseMessage> RequestTokenAsync(string username, string password)
	{
		using var request = new HttpRequestMessage(HttpMethod.Post, "/generate-token");
		request.Content = JsonContent.Create(new { username, password });
		
		request.Headers.Add("Membership", this.MembershipId);
		return await this.CreateClient().SendAsync(request);
	}
	
	public async Task<(string AccessToken, string RefreshToken)> GenerateTokenAsync(string username = AdminUsername, string password = AdminPassword)
	{
		using var response = await this.RequestTokenAsync(username, password);
		var token = await ReadJsonAsync(response);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"generate-token failed ({(int) response.StatusCode}): {token}");
		}
		
		return (token.GetProperty("access_token").GetString()!, token.GetProperty("refresh_token").GetString()!);
	}
	
	public async Task<HttpClient> CreateAdminClientAsync()
	{
		var (accessToken, _) = await this.GenerateTokenAsync();
		return this.CreateClient($"Bearer {accessToken}");
	}
	
	/// <summary>
	/// Changes the membership through the API (GET, modify, PUT), so the service's cache stays consistent.
	/// </summary>
	public async Task UpdateMembershipAsync(Action<JsonObject> change)
	{
		var adminClient = await this.CreateAdminClientAsync();
		var membership = await adminClient.GetFromJsonAsync<JsonObject>($"/memberships/{this.MembershipId}");
		change(membership!);
		using var response = await adminClient.PutAsJsonAsync($"/memberships/{this.MembershipId}", membership);
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"Membership update failed ({(int) response.StatusCode}): {await ReadJsonAsync(response)}");
		}
	}
	
	public async Task<string> GetAdminUserIdAsync()
	{
		var adminClient = await this.CreateAdminClientAsync();
		var me = await adminClient.GetFromJsonAsync<JsonObject>("/me");
		return me!["_id"]!.GetValue<string>();
	}
	
	#endregion
	
	#region Helpers
	
	public static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
	{
		var content = await response.Content.ReadAsStringAsync();
		return string.IsNullOrWhiteSpace(content) ? default : JsonDocument.Parse(content).RootElement.Clone();
	}
	
	#endregion
}

/// <summary>
/// An installation that is not set up yet.
/// </summary>
public sealed class FreshErtisAuthInstance : ErtisAuthInstance
{
	#region Properties
	
	protected override bool SetUpOnStart => false;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mongo"></param>
	public FreshErtisAuthInstance(MongoDbContainerFixture mongo) : base(mongo)
	{
	
	}
	
	#endregion
}