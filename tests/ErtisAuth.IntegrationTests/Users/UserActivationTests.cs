using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Web;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MimeKit;

namespace ErtisAuth.IntegrationTests.Users;

/// <summary>
/// User activation on a membership with user_activation: new users are inactive until they follow the link in the
/// activation mail; administrators can activate and freeze users by id.
/// </summary>
public partial class UserActivationTests : IClassFixture<ActivationErtisAuthInstance>
{
	#region Constants
	
	private const string Password = "Initial-P@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	private readonly ActivationErtisAuthInstance _instance;
	
	#endregion
	
	#region Properties
	
	private string UsersUrl => $"/memberships/{this._instance.MembershipId}/users";
	
	private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="instance"></param>
	public UserActivationTests(ActivationErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<(JsonObject User, string Username, string EmailAddress)> CreateUserAsync(string? host = MailingErtisAuthInstance.Host)
	{
		var username = $"user{Guid.NewGuid():N}";
		var emailAddress = $"{username}@example.com";
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var request = new HttpRequestMessage(HttpMethod.Post, this.UsersUrl);
		request.Content = JsonContent.Create(new
		{
			username,
			firstname = "Jane",
			lastname = "Doe",
			email_address = emailAddress,
			password = Password,
			role = "admin",
			user_type = "user"
		});
		
		if (host != null)
		{
			request.Headers.Add("X-Host", host);
		}
		
		using var response = await adminClient.SendAsync(request, CancellationToken);
		var created = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
		return (created!.AsObject(), username, emailAddress);
	}
	
	/// <summary>
	/// The activation code as a browser reads it from the link's query string.
	/// </summary>
	private static string ReadActivationCode(MimeMessage message)
	{
		var link = HttpUtility.HtmlDecode(LinkRegex().Match(message.HtmlBody ?? string.Empty).Groups["link"].Value);
		Assert.StartsWith($"{MailingErtisAuthInstance.Host}?uat=", link);
		
		var code = HttpUtility.ParseQueryString(new Uri(link).Query)["uat"];
		Assert.False(string.IsNullOrEmpty(code));
		return code;
	}
	
	private async Task<HttpResponseMessage> ActivateAsync(string code)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.GetAsync($"{this.UsersUrl}/activation?uat={Uri.EscapeDataString(code)}", CancellationToken);
	}
	
	[GeneratedRegex("href=\"(?<link>[^\"]+)\"")]
	private static partial Regex LinkRegex();
	
	#endregion
	
	#region Activation Link
	
	[Fact]
	public async Task CreatedUser_IsInactiveUntilActivatedWithTheLinkFromTheMail()
	{
		var (user, username, emailAddress) = await this.CreateUserAsync();
		Assert.False(user["is_active"]!.GetValue<bool>());
		
		using var inactiveLogin = await this._instance.RequestTokenAsync(username, Password);
		Assert.False(inactiveLogin.IsSuccessStatusCode, "An inactive user logged in");
		
		var mail = await this._instance.Smtp.WaitForMessageAsync(emailAddress, x => x.Subject == "Activate your account");
		var code = ReadActivationCode(mail);
		
		using var activationResponse = await this.ActivateAsync(code);
		var activated = await ResourceClient.AssertStatusAsync(activationResponse, HttpStatusCode.OK);
		Assert.True(activated!["is_active"]!.GetValue<bool>());
		
		using var activeLogin = await this._instance.RequestTokenAsync(username, Password);
		await ResourceClient.AssertStatusAsync(activeLogin, HttpStatusCode.Created);
		
		// Single use
		using var reuseResponse = await this.ActivateAsync(code);
		var reuse = await ResourceClient.AssertStatusAsync(reuseResponse, reuseResponse.StatusCode);
		Assert.False(reuseResponse.IsSuccessStatusCode);
		Assert.Equal("UserAlreadyActive", reuse!["errorCode"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task CreatedUser_SendsASingleActivationMail()
	{
		var (_, _, emailAddress) = await this.CreateUserAsync();
		await this._instance.Smtp.WaitForMessageAsync(emailAddress);
		
		// Mails are sent in the background: give a second mail the time to arrive
		await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		Assert.Single(this._instance.Smtp.MessagesTo(emailAddress));
	}
	
	[Fact]
	public async Task ResendActivationMail_SendsAnotherLink()
	{
		// Without X-Host no link can be built, so no mail is sent on create
		var (_, _, emailAddress) = await this.CreateUserAsync(host: null);
		
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var request = new HttpRequestMessage(HttpMethod.Post, $"{this.UsersUrl}/resend-activation-mail");
		request.Content = JsonContent.Create(new { email_address = emailAddress });
		
		request.Headers.Add("X-Host", MailingErtisAuthInstance.Host);
		using var response = await adminClient.SendAsync(request, CancellationToken);
		var body = await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		Assert.Equal(emailAddress, body!["emailAddress"]!.GetValue<string>());
		
		var code = ReadActivationCode(await this._instance.Smtp.WaitForMessageAsync(emailAddress));
		using var activationResponse = await this.ActivateAsync(code);
		await ResourceClient.AssertStatusAsync(activationResponse, HttpStatusCode.OK);
	}
	
	[Fact]
	public async Task Activation_WithInvalidCode_ReturnsUnauthorized()
	{
		using var response = await this.ActivateAsync(Convert.ToBase64String("not-a-code"u8.ToArray()));
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
	}
	
	#endregion
	
	#region Manual Activation
	
	[Fact]
	public async Task FreezeAndActivateById()
	{
		var (user, username, _) = await this.CreateUserAsync(host: null);
		var id = user["_id"]!.GetValue<string>();
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var activateResponse = await adminClient.GetAsync($"{this.UsersUrl}/{id}/activate", CancellationToken);
		var activated = await ResourceClient.AssertStatusAsync(activateResponse, HttpStatusCode.OK);
		Assert.True(activated!["is_active"]!.GetValue<bool>());
		
		using var activateAgainResponse = await adminClient.GetAsync($"{this.UsersUrl}/{id}/activate", CancellationToken);
		var activateAgain = await ResourceClient.AssertStatusAsync(activateAgainResponse, activateAgainResponse.StatusCode);
		Assert.False(activateAgainResponse.IsSuccessStatusCode);
		Assert.Equal("UserAlreadyActive", activateAgain!["errorCode"]!.GetValue<string>());
		
		// Freezing revokes the user's tokens and blocks new logins
		var (accessToken, _) = await this._instance.GenerateTokenAsync(username, Password);
		using var freezeResponse = await adminClient.GetAsync($"{this.UsersUrl}/{id}/freeze", CancellationToken);
		var frozen = await ResourceClient.AssertStatusAsync(freezeResponse, HttpStatusCode.OK);
		Assert.False(frozen!["is_active"]!.GetValue<bool>());
		
		using var meResponse = await this._instance.CreateClient($"Bearer {accessToken}").GetAsync("/me", CancellationToken);
		Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
		using var frozenLogin = await this._instance.RequestTokenAsync(username, Password);
		Assert.False(frozenLogin.IsSuccessStatusCode, "A frozen user logged in");
		
		using var freezeAgainResponse = await adminClient.GetAsync($"{this.UsersUrl}/{id}/freeze", CancellationToken);
		var freezeAgain = await ResourceClient.AssertStatusAsync(freezeAgainResponse, freezeAgainResponse.StatusCode);
		Assert.False(freezeAgainResponse.IsSuccessStatusCode);
		Assert.Equal("UserAlreadyInactive", freezeAgain!["errorCode"]!.GetValue<string>());
	}
	
	#endregion
}