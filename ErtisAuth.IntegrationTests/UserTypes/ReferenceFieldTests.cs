using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;

namespace ErtisAuth.IntegrationTests.UserTypes;

/// <summary>
/// Reference fields of user types point to other users by id. With a content type, the referenced users must be of that
/// user type (or inherit from it) and are embedded into the user (password hash excluded).
/// </summary>
public class ReferenceFieldTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string MembershipUrl => $"/memberships/{this._instance.MembershipId}";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	public ReferenceFieldTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<string> CreateUserTypeAsync(string baseType = "user", JsonObject? properties = null)
	{
		var userTypes = new ResourceClient(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/user-types");
		var userType = await userTypes.CreateAsync(new JsonObject
		{
			["name"] = $"Reference {Guid.NewGuid():N}",
			["baseType"] = baseType,
			["properties"] = properties ?? new JsonObject()
		});
		
		return userType["slug"]!.GetValue<string>();
	}
	
	private async Task<string> CreateReferencingUserTypeAsync(string referenceType, string? contentType)
	{
		var field = new JsonObject { ["type"] = "reference", ["referenceType"] = referenceType };
		if (contentType != null)
		{
			field["contentType"] = contentType;
		}
		
		return await this.CreateUserTypeAsync(properties: new JsonObject { ["ref"] = field });
	}
	
	private async Task<HttpResponseMessage> CreateUserAsync(string userType, JsonNode? reference = null)
	{
		var username = $"user{Guid.NewGuid():N}";
		var body = new JsonObject
		{
			["username"] = username,
			["firstname"] = "Reference",
			["lastname"] = "User",
			["email_address"] = $"{username}@example.com",
			["password"] = "Reference-P@ssw0rd!",
			["role"] = "admin",
			["user_type"] = userType
		};
		
		if (reference != null)
		{
			body["ref"] = reference;
		}
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync($"{this.MembershipUrl}/users", body, CancellationToken);
	}
	
	private async Task<string> CreateUserIdAsync(string userType)
	{
		using var response = await this.CreateUserAsync(userType);
		var user = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		return user!["_id"]!.GetValue<string>();
	}
	
	private async Task<JsonObject> CreateReferencingUserAsync(string userType, JsonNode reference)
	{
		using var response = await this.CreateUserAsync(userType, reference);
		var user = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		
		// Read back: what is stored
		var users = new ResourceClient(await this._instance.CreateAdminClientAsync(), $"{this.MembershipUrl}/users");
		return await users.GetAsync(user!["_id"]!.GetValue<string>());
	}
	
	private static async Task AssertRejectedAsync(HttpResponseMessage response, string message)
	{
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		var json = error!.ToJsonString(new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
		Assert.Contains(message, json);
	}
	
	#endregion
	
	#region Single
	
	[Fact]
	public async Task SingleReference_WithContentType_EmbedsTheReferencedUser()
	{
		var contentType = await this.CreateUserTypeAsync();
		var referencedId = await this.CreateUserIdAsync(contentType);
		var userType = await this.CreateReferencingUserTypeAsync("single", contentType);
		
		var user = await this.CreateReferencingUserAsync(userType, referencedId);
		
		var embedded = user["ref"]!.AsObject();
		Assert.Equal(referencedId, embedded["_id"]!.GetValue<string>());
		Assert.Equal(contentType, embedded["user_type"]!.GetValue<string>());
		Assert.Null(embedded["password_hash"]);
	}
	
	[Fact]
	public async Task SingleReference_ToAnInheritedType_IsAccepted()
	{
		var contentType = await this.CreateUserTypeAsync();
		var derivedType = await this.CreateUserTypeAsync(baseType: contentType);
		var referencedId = await this.CreateUserIdAsync(derivedType);
		var userType = await this.CreateReferencingUserTypeAsync("single", contentType);
		
		var user = await this.CreateReferencingUserAsync(userType, referencedId);
		
		Assert.Equal(referencedId, user["ref"]!["_id"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task SingleReference_ToAnotherType_IsRejected()
	{
		var contentType = await this.CreateUserTypeAsync();
		var otherType = await this.CreateUserTypeAsync();
		var referencedId = await this.CreateUserIdAsync(otherType);
		var userType = await this.CreateReferencingUserTypeAsync("single", contentType);
		
		using var response = await this.CreateUserAsync(userType, referencedId);
		
		await AssertRejectedAsync(response, $"This reference-type field only can bind contents from '{contentType}' content-type");
	}
	
	[Fact]
	public async Task SingleReference_ToAMissingUser_IsRejected()
	{
		var contentType = await this.CreateUserTypeAsync();
		var userType = await this.CreateReferencingUserTypeAsync("single", contentType);
		const string missingId = "5f0a1b2c3d4e5f6a7b8c9d0e";
		
		using var response = await this.CreateUserAsync(userType, missingId);
		
		await AssertRejectedAsync(response, $"Could not find any content with id '{missingId}' for reference type 'ref'");
	}
	
	/// <summary>
	/// Without a content type the referenced user must exist, but the id is kept as it is (not embedded).
	/// </summary>
	[Fact]
	public async Task SingleReference_WithoutContentType_KeepsTheId()
	{
		var referencedId = await this.CreateUserIdAsync(await this.CreateUserTypeAsync());
		var userType = await this.CreateReferencingUserTypeAsync("single", null);
		
		var user = await this.CreateReferencingUserAsync(userType, referencedId);
		
		Assert.Equal(referencedId, user["ref"]!.GetValue<string>());
	}
	
	#endregion
	
	#region Multiple
	
	[Fact]
	public async Task MultipleReference_WithContentType_EmbedsTheReferencedUsersInOrder()
	{
		var contentType = await this.CreateUserTypeAsync();
		var firstId = await this.CreateUserIdAsync(contentType);
		var secondId = await this.CreateUserIdAsync(contentType);
		var userType = await this.CreateReferencingUserTypeAsync("multiple", contentType);
		
		var user = await this.CreateReferencingUserAsync(userType, new JsonArray(secondId, firstId));
		
		var embedded = user["ref"]!.AsArray();
		Assert.Equal([secondId, firstId], embedded.Select(x => x!["_id"]!.GetValue<string>()));
		Assert.All(embedded, x => Assert.Null(x!["password_hash"]));
	}
	
	[Fact]
	public async Task MultipleReference_WithOneOfAnotherType_IsRejected()
	{
		var contentType = await this.CreateUserTypeAsync();
		var validId = await this.CreateUserIdAsync(contentType);
		var otherId = await this.CreateUserIdAsync(await this.CreateUserTypeAsync());
		var userType = await this.CreateReferencingUserTypeAsync("multiple", contentType);
		
		using var response = await this.CreateUserAsync(userType, new JsonArray(validId, otherId));
		
		await AssertRejectedAsync(response, $"This reference-type field only can bind contents from '{contentType}' content-type");
	}
	
	[Fact]
	public async Task MultipleReference_WithAMissingUser_IsRejected()
	{
		var contentType = await this.CreateUserTypeAsync();
		var validId = await this.CreateUserIdAsync(contentType);
		var userType = await this.CreateReferencingUserTypeAsync("multiple", contentType);
		const string missingId = "5f0a1b2c3d4e5f6a7b8c9d0e";
		
		using var response = await this.CreateUserAsync(userType, new JsonArray(validId, missingId));
		
		await AssertRejectedAsync(response, $"Could not find any content with id '{missingId}' for reference type 'ref'");
	}
	
	/// <summary>
	/// Without a content type the referenced users must exist, but the ids are kept as they are (not embedded), like the single
	/// reference. Before, the ids were replaced by an empty array (the references were lost; also on master).
	/// </summary>
	[Fact]
	public async Task MultipleReference_WithoutContentType_KeepsTheIds()
	{
		var contentType = await this.CreateUserTypeAsync();
		var firstId = await this.CreateUserIdAsync(contentType);
		var secondId = await this.CreateUserIdAsync(contentType);
		var userType = await this.CreateReferencingUserTypeAsync("multiple", null);
		
		var user = await this.CreateReferencingUserAsync(userType, new JsonArray(secondId, firstId));
		
		Assert.Equal([secondId, firstId], user["ref"]!.AsArray().Select(x => x!.GetValue<string>()));
	}
	
	[Fact]
	public async Task MultipleReference_WithoutContentType_ToAMissingUser_IsRejected()
	{
		var referencedId = await this.CreateUserIdAsync(await this.CreateUserTypeAsync());
		var userType = await this.CreateReferencingUserTypeAsync("multiple", null);
		const string missingId = "5f0a1b2c3d4e5f6a7b8c9d0e";
		
		using var response = await this.CreateUserAsync(userType, new JsonArray(referencedId, missingId));
		
		await AssertRejectedAsync(response, $"Could not find any content with id '{missingId}' for reference type 'ref'");
	}
	
	#endregion
}
