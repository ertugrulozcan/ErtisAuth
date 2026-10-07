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
	/// The default slug of a provider created by <see cref="ConfigureProviderAsync"/>: its type in lower case.
	/// </summary>
	public static string SlugOf(string type) => type.ToLowerInvariant();
	
	/// <summary>
	/// The membership's provider of the given type created by <see cref="ConfigureProviderAsync"/> (found by its slug,
	/// by default the type in lower case), or null.
	/// </summary>
	public async Task<JsonObject?> FindProviderAsync(string type, string? slug = null)
	{
		var adminClient = await this.CreateAdminClientAsync();
		using var response = await adminClient.GetAsync($"/memberships/{this.MembershipId}/providers/{slug ?? SlugOf(type)}");
		if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
		{
			return null;
		}
		
		response.EnsureSuccessStatusCode();
		return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
	}
	
	/// <summary>
	/// Creates (POST) or updates (PUT) the provider of the given type: active, default role 'admin' and user type
	/// 'user', plus the given fields. Configuring it again with the same values is a no-op (the API answers IdenticalDocumentError).
	/// The provider is found (and created) by the given slug, by default the type in lower case.
	/// </summary>
	public async Task<JsonObject> ConfigureProviderAsync(string type, Action<JsonObject> configure, string? slug = null)
	{
		var model = new JsonObject
		{
			["defaultRole"] = "admin",
			["defaultUserType"] = "user",
			["isActive"] = true
		};
		
		configure(model);
		
		var adminClient = await this.CreateAdminClientAsync();
		var provider = await this.FindProviderAsync(type, slug);
		HttpResponseMessage response;
		if (provider == null)
		{
			model["type"] = type;
			model["slug"] = slug ?? SlugOf(type);
			response = await adminClient.PostAsJsonAsync($"/memberships/{this.MembershipId}/providers", model);
		}
		else
		{
			response = await adminClient.PutAsJsonAsync($"/memberships/{this.MembershipId}/providers/{provider["_id"]!.GetValue<string>()}", model);
		}
		
		using (response)
		{
			if (response.StatusCode == System.Net.HttpStatusCode.Conflict && (await ReadJsonAsync(response)).GetProperty("errorCode").GetString() == "IdenticalDocumentError")
			{
				return provider!;
			}
			
			if (!response.IsSuccessStatusCode)
			{
				throw new InvalidOperationException($"Provider '{type}' could not be configured ({(int) response.StatusCode}): {await ReadJsonAsync(response)}");
			}
			
			return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
		}
	}
	
	#endregion
}