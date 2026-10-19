using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ErtisAuth.IntegrationTests.Users;

/// <summary>
/// The login accepts a username or an email address, so the username of a user can not be the email address of another user
/// and vice versa. A user's own username may be its own email address. Both are stored trimmed and lower cased, and looked up
/// the same way.
/// </summary>
public class LoginNameTests : IClassFixture<ErtisAuthInstance>
{
	#region Fields
	
	private readonly ErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string UsersUrl => $"/memberships/{this._instance.MembershipId}/users";
	
	private IMongoCollection<BsonDocument> Users => this._instance.Database.GetCollection<BsonDocument>("users");
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public LoginNameTests(ErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private static JsonObject UserBody(string username, string emailAddress) => new()
	{
		["username"] = username,
		["firstname"] = "Jane",
		["lastname"] = "Doe",
		["email_address"] = emailAddress,
		["password"] = "Login-P@ssw0rd!",
		["role"] = "admin",
		["user_type"] = "user"
	};
	
	private static string NewUsername() => $"user{Guid.NewGuid():N}";
	
	private static string NewEmailAddress() => $"{Guid.NewGuid():N}@example.com";
	
	private async Task<HttpResponseMessage> PostUserAsync(string username, string emailAddress)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync(this.UsersUrl, UserBody(username, emailAddress), CancellationToken);
	}
	
	private async Task<HttpResponseMessage> PutUserAsync(string id, object body)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PutAsJsonAsync($"{this.UsersUrl}/{id}", body, CancellationToken);
	}
	
	private async Task<(string Id, string Username, string EmailAddress)> CreateUserAsync(string? username = null, string? emailAddress = null)
	{
		username ??= NewUsername();
		emailAddress ??= NewEmailAddress();
		var users = new ResourceClient(await this._instance.CreateAdminClientAsync(), this.UsersUrl);
		var created = await users.CreateAsync(UserBody(username, emailAddress));
		return (created["_id"]!.GetValue<string>(), username, emailAddress);
	}
	
	private static async Task AssertLoginNameErrorAsync(HttpResponseMessage response, string field, string otherField)
	{
		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		var json = error!.ToJsonString(new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
		Assert.Contains($"The '{field}' field can not be the {otherField} of another user.", json);
	}
	
	#endregion
	
	#region Create
	
	[Fact]
	public async Task Create_WithTheEmailAddressOfAnotherUserAsUsername_IsRejected()
	{
		var other = await this.CreateUserAsync();
		
		using var response = await this.PostUserAsync(other.EmailAddress, NewEmailAddress());
		
		await AssertLoginNameErrorAsync(response, "username", "email_address");
	}
	
	[Fact]
	public async Task Create_WithTheUsernameOfAnotherUserAsEmailAddress_IsRejected()
	{
		var other = await this.CreateUserAsync(username: NewEmailAddress());
		
		using var response = await this.PostUserAsync(NewUsername(), other.Username);
		
		await AssertLoginNameErrorAsync(response, "email_address", "username");
	}
	
	[Fact]
	public async Task Create_WithItsOwnEmailAddressAsUsername_IsAllowed()
	{
		var emailAddress = NewEmailAddress();
		
		var created = await this.CreateUserAsync(emailAddress, emailAddress);
		
		Assert.Equal(created.EmailAddress, created.Username);
	}
	
	#endregion
	
	#region Update
	
	[Fact]
	public async Task Update_UsernameToTheEmailAddressOfAnotherUser_IsRejected()
	{
		var other = await this.CreateUserAsync();
		var user = await this.CreateUserAsync();
		
		using var response = await this.PutUserAsync(user.Id, new { username = other.EmailAddress });
		
		await AssertLoginNameErrorAsync(response, "username", "email_address");
	}
	
	[Fact]
	public async Task Update_EmailAddressToTheUsernameOfAnotherUser_IsRejected()
	{
		var other = await this.CreateUserAsync(username: NewEmailAddress());
		var user = await this.CreateUserAsync();
		
		using var response = await this.PutUserAsync(user.Id, new { email_address = other.Username });
		
		await AssertLoginNameErrorAsync(response, "email_address", "username");
	}
	
	[Fact]
	public async Task Update_UsernameToItsOwnEmailAddress_IsAllowed()
	{
		var user = await this.CreateUserAsync();
		
		using var response = await this.PutUserAsync(user.Id, new { username = user.EmailAddress });
		
		var updated = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		Assert.Equal(user.EmailAddress, updated!["username"]!.GetValue<string>());
	}
	
	/// <summary>
	/// Users already in conflict (written before the check existed) can still be updated, as long as the conflicting value is kept.
	/// </summary>
	[Fact]
	public async Task Update_OfAUserAlreadyInConflict_IsAllowedWhenTheLoginNamesAreKept()
	{
		var other = await this.CreateUserAsync();
		var user = await this.CreateUserAsync();
		await this.Users.UpdateOneAsync(
			Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(user.Id)),
			Builders<BsonDocument>.Update.Set("username", other.EmailAddress),
			cancellationToken: CancellationToken);
		
		using var response = await this.PutUserAsync(user.Id, new { firstname = "Changed" });
		
		var updated = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		Assert.Equal("Changed", updated!["firstname"]!.GetValue<string>());
		Assert.Equal(other.EmailAddress, updated["username"]!.GetValue<string>());
	}

	#endregion

	#region Normalization

	[Fact]
	public async Task Create_StoresTheLoginNamesTrimmedAndLowerCased()
	{
		var username = NewUsername();
		var emailAddress = NewEmailAddress();

		using var response = await this.PostUserAsync($"  {username.ToUpperInvariant()} ", $" {emailAddress.ToUpperInvariant()}  ");

		var created = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		Assert.Equal(username, created!["username"]!.GetValue<string>());
		Assert.Equal(emailAddress, created["email_address"]!.GetValue<string>());
	}

	[Fact]
	public async Task Update_StoresTheLoginNamesTrimmedAndLowerCased()
	{
		var user = await this.CreateUserAsync();
		var username = NewUsername();
		var emailAddress = NewEmailAddress();

		using var response = await this.PutUserAsync(user.Id, new { username = $" {username.ToUpperInvariant()}", email_address = $"{emailAddress.ToUpperInvariant()} " });

		var updated = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		Assert.Equal(username, updated!["username"]!.GetValue<string>());
		Assert.Equal(emailAddress, updated["email_address"]!.GetValue<string>());
	}

	[Fact]
	public async Task Create_WithAnExistingUsernameInAnotherCase_IsRejected()
	{
		var other = await this.CreateUserAsync();

		using var response = await this.PostUserAsync(other.Username.ToUpperInvariant(), NewEmailAddress());

		var error = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		var json = error!.ToJsonString(new System.Text.Json.JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
		Assert.Contains("The 'username' field has unique constraint", json);
	}

	[Fact]
	public async Task Create_WithTheEmailAddressOfAnotherUserInAnotherCaseAsUsername_IsRejected()
	{
		var other = await this.CreateUserAsync();

		using var response = await this.PostUserAsync(other.EmailAddress.ToUpperInvariant(), NewEmailAddress());

		await AssertLoginNameErrorAsync(response, "username", "email_address");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Login_IgnoresTheCaseAndTheSurroundingWhiteSpace(bool byEmailAddress)
	{
		var user = await this.CreateUserAsync();
		var loginName = byEmailAddress ? user.EmailAddress : user.Username;

		using var response = await this._instance.RequestTokenAsync($" {loginName.ToUpperInvariant()} ", "Login-P@ssw0rd!");

		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
	}

	#endregion
}
