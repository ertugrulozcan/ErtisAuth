using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.Integrations.OAuth.Google;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// An installation whose calls to the OAuth providers are answered by <see cref="FakeOAuthProviders"/>:
/// the provider login runs end to end (controller, ProviderService, the real authenticators, HTTP).
/// </summary>
public sealed class OAuthErtisAuthInstance : ErtisAuthInstance
{
	#region Properties
	
	public FakeOAuthProviders Providers { get; } = new();
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="mongo"></param>
	public OAuthErtisAuthInstance(MongoDbContainerFixture mongo) : base(mongo)
	{
	
	}
	
	#endregion
	
	#region Methods
	
	protected override void ConfigureTestServices(IServiceCollection services)
	{
		services.ConfigureHttpClientDefaults(builder => builder.ConfigurePrimaryHttpMessageHandler(this.Providers.CreateHandler));
		services.RemoveAll<IGoogleIdTokenValidator>();
		services.AddSingleton<IGoogleIdTokenValidator>(this.Providers.Google);
	}
	
	/// <summary>
	/// The membership's provider of the given name (the providers are created on the first listing).
	/// </summary>
	public async Task<JsonObject> GetProviderAsync(string name)
	{
		var adminClient = await this.CreateAdminClientAsync();
		var providers = await adminClient.GetFromJsonAsync<JsonArray>($"/memberships/{this.MembershipId}/providers");
		return providers!.Single(x => x!["name"]!.GetValue<string>() == name)!.AsObject();
	}
	
	/// <summary>
	/// Configures (PUT) the provider of the given name: active, default role 'admin' and user type 'user', plus the
	/// given fields. Configuring it again with the same values is a no-op (the API answers IdenticalDocumentError).
	/// </summary>
	public async Task<JsonObject> ConfigureProviderAsync(string name, Action<JsonObject> configure)
	{
		var provider = await this.GetProviderAsync(name);
		var id = provider["_id"]!.GetValue<string>();
		var model = new JsonObject
		{
			["name"] = name,
			["defaultRole"] = "admin",
			["defaultUserType"] = "user",
			["isActive"] = true
		};
		
		configure(model);
		
		var adminClient = await this.CreateAdminClientAsync();
		using var response = await adminClient.PutAsJsonAsync($"/memberships/{this.MembershipId}/providers/{id}", model);
		if (response.StatusCode == System.Net.HttpStatusCode.Conflict && (await ReadJsonAsync(response)).GetProperty("errorCode").GetString() == "IdenticalDocumentError")
		{
			return await this.GetProviderAsync(name);
		}
		
		if (!response.IsSuccessStatusCode)
		{
			throw new InvalidOperationException($"Provider '{name}' could not be configured ({(int) response.StatusCode}): {await ReadJsonAsync(response)}");
		}
		
		return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
	}
	
	#endregion
}