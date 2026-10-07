using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Memberships;

/// <summary>
/// 'mail_providers' is an IMailProvider[]: the concrete provider is chosen by its 'type' in JSON (MailProviderJsonConverter)
/// and by the 'type' discriminator in BSON (MailProviderDiscriminatorConvention).
/// </summary>
public class MembershipMailProviderTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public MembershipMailProviderTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static JsonArray AllProviders => new()
	{
		new JsonObject
		{
			["type"] = "SmtpServer",
			["guid"] = "smtp-guid",
			["name"] = "Smtp Provider",
			["host"] = "smtp.example.com",
			["port"] = 587,
			["tls_enabled"] = true,
			["username"] = "smtp-user",
			["password"] = "smtp-password"
		},
		new JsonObject
		{
			["type"] = "SendGrid",
			["guid"] = "sendgrid-guid",
			["name"] = "SendGrid Provider",
			["apiKey"] = "sendgrid-api-key"
		},
		new JsonObject
		{
			["type"] = "MailChimp",
			["guid"] = "mailchimp-guid",
			["name"] = "MailChimp Provider",
			["apiKey"] = "mailchimp-api-key"
		}
	};
	
	private async Task<(HttpStatusCode StatusCode, JsonNode? Body)> PutMailProvidersAsync(HttpClient adminClient, JsonNode? mailProviders)
	{
		var membership = await adminClient.GetFromJsonAsync<JsonObject>(this.MembershipUrl, TestContext.Current.CancellationToken);
		membership!["mail_providers"] = mailProviders;
		
		using var response = await adminClient.PutAsJsonAsync(this.MembershipUrl, membership, TestContext.Current.CancellationToken);
		var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
		return (response.StatusCode, string.IsNullOrWhiteSpace(content) ? null : JsonNode.Parse(content));
	}
	
	private static void AssertProviders(JsonNode? mailProviders)
	{
		var providers = mailProviders!.AsArray();
		Assert.Equal(3, providers.Count);
		
		var smtp = providers[0]!;
		Assert.Equal("SmtpServer", smtp["type"]!.GetValue<string>());
		Assert.Equal("Default", smtp["deliveryMode"]!.GetValue<string>());
		Assert.Equal("smtp-guid", smtp["guid"]!.GetValue<string>());
		Assert.Equal("Smtp Provider", smtp["name"]!.GetValue<string>());
		Assert.Equal("smtp-provider", smtp["slug"]!.GetValue<string>());
		Assert.Equal("smtp.example.com", smtp["host"]!.GetValue<string>());
		Assert.Equal(587, smtp["port"]!.GetValue<int>());
		Assert.True(smtp["tls_enabled"]!.GetValue<bool>());
		Assert.Equal("smtp-user", smtp["username"]!.GetValue<string>());
		Assert.Equal("smtp-password", smtp["password"]!.GetValue<string>());
		
		var sendGrid = providers[1]!;
		Assert.Equal("SendGrid", sendGrid["type"]!.GetValue<string>());
		Assert.Equal("Default", sendGrid["deliveryMode"]!.GetValue<string>());
		Assert.Equal("sendgrid-provider", sendGrid["slug"]!.GetValue<string>());
		Assert.Equal("sendgrid-api-key", sendGrid["apiKey"]!.GetValue<string>());
		
		var mailChimp = providers[2]!;
		Assert.Equal("MailChimp", mailChimp["type"]!.GetValue<string>());
		Assert.Equal("Template", mailChimp["deliveryMode"]!.GetValue<string>());
		Assert.Equal("mailchimp-provider", mailChimp["slug"]!.GetValue<string>());
		Assert.Equal("mailchimp-api-key", mailChimp["apiKey"]!.GetValue<string>());
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task UpdateMembership_WithAllProviderTypes_RoundTripsThroughTheDatabase()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		var (statusCode, updated) = await this.PutMailProvidersAsync(adminClient, AllProviders);
		Assert.True(statusCode == HttpStatusCode.OK, $"{(int) statusCode}: {updated}");
		AssertProviders(updated!["mail_providers"]);
		
		// Read back through the API
		var fetched = await adminClient.GetFromJsonAsync<JsonObject>(this.MembershipUrl, TestContext.Current.CancellationToken);
		AssertProviders(fetched!["mail_providers"]);
		
		// Stored with the BSON 'type' discriminator
		var document = await this._instance.Database.GetCollection<BsonDocument>("memberships")
			.Find(new BsonDocument("_id", ObjectId.Parse(this._instance.MembershipId)))
			.FirstAsync(TestContext.Current.CancellationToken);
		var stored = document["mail_providers"].AsBsonArray.Select(x => x["type"].AsString).ToArray();
		Assert.Equal(["SmtpServer", "SendGrid", "MailChimp"], stored);
	}
	
	[Fact]
	public async Task UpdateMembership_TypeAfterNestedTypeProperty_UsesTheTopLevelType()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		var provider = new JsonObject
		{
			["guid"] = "sendgrid-guid",
			["extra"] = new JsonObject { ["type"] = "SmtpServer" },
			["name"] = "SendGrid Provider",
			["apiKey"] = "sendgrid-api-key",
			["type"] = "SendGrid"
		};
		
		var (statusCode, updated) = await this.PutMailProvidersAsync(adminClient, new JsonArray { provider });
		Assert.True(statusCode == HttpStatusCode.OK, $"{(int) statusCode}: {updated}");
		Assert.Equal("SendGrid", updated!["mail_providers"]![0]!["type"]!.GetValue<string>());
		Assert.Equal("sendgrid-api-key", updated["mail_providers"]![0]!["apiKey"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task UpdateMembership_WithoutMailProviders_Succeeds()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		var (statusCode, updated) = await this.PutMailProvidersAsync(adminClient, null);
		Assert.True(statusCode == HttpStatusCode.OK, $"{(int) statusCode}: {updated}");
		Assert.Null(updated!["mail_providers"]);
	}
	
	[Theory]
	[InlineData("UnknownProvider")]
	[InlineData("smtpserver")]
	[InlineData("")]
	[InlineData(null)]
	public async Task UpdateMembership_WithInvalidProviderType_ReturnsBadRequest(string? type)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		var provider = new JsonObject
		{
			["name"] = "Invalid Provider",
			["apiKey"] = "api-key"
		};
		
		if (type != null)
		{
			provider["type"] = type;
		}
		
		var (statusCode, body) = await this.PutMailProvidersAsync(adminClient, new JsonArray { provider });
		Assert.True(statusCode == HttpStatusCode.BadRequest, $"{(int) statusCode}: {body}");
	}
	
	[Fact]
	public async Task UpdateMembership_WithMissingRequiredProviderField_ReturnsBadRequest()
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		// SmtpServerProvider.Host is required
		var provider = new JsonObject
		{
			["type"] = "SmtpServer",
			["name"] = "Smtp Provider",
			["port"] = 587,
			["username"] = "smtp-user",
			["password"] = "smtp-password"
		};
		
		var (statusCode, body) = await this.PutMailProvidersAsync(adminClient, new JsonArray { provider });
		Assert.True(statusCode == HttpStatusCode.BadRequest, $"{(int) statusCode}: {body}");
	}
	
	#endregion
}
