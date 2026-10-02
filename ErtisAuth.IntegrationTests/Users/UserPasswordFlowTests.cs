using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Web;
using ErtisAuth.IntegrationTests.Infrastructure;
using ErtisAuth.IntegrationTests.Resources;
using MimeKit;

namespace ErtisAuth.IntegrationTests.Users;

/// <summary>
/// Password change, password check and the forgot password flow (reset mail -> verify-reset-token -> set-password),
/// with the reset link read from the mail as the user's browser would.
/// </summary>
public partial class UserPasswordFlowTests : IClassFixture<MailingErtisAuthInstance>
{
	#region Constants
	
	private const string Password = "Initial-P@ssw0rd!";
	
	private const string NewPassword = "Changed-P@ssw0rd!";
	
	#endregion
	
	#region Fields
	
	private readonly MailingErtisAuthInstance _instance;
	
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
	public UserPasswordFlowTests(MailingErtisAuthInstance instance)
	{
		this._instance = instance;
	}
	
	#endregion
	
	#region Helpers
	
	private async Task<(string Id, string Username, string EmailAddress)> CreateUserAsync()
	{
		var username = $"user{Guid.NewGuid():N}";
		var emailAddress = $"{username}@example.com";
		var users = new ResourceClient(await this._instance.CreateAdminClientAsync(), this.UsersUrl);
		var created = await users.CreateAsync(new
		{
			username,
			firstname = "Jane",
			lastname = "Doe",
			email_address = emailAddress,
			password = Password,
			role = "admin",
			user_type = "user"
		});
		
		return (created["_id"]!.GetValue<string>(), username, emailAddress);
	}
	
	private async Task AssertCanLoginAsync(string username, string password)
	{
		using var response = await this._instance.RequestTokenAsync(username, password);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.Created);
	}
	
	private async Task AssertCanNotLoginAsync(string username, string password)
	{
		using var response = await this._instance.RequestTokenAsync(username, password);
		Assert.False(response.IsSuccessStatusCode, $"{username} logged in with the password '{password}'");
	}
	
	private async Task<HttpResponseMessage> RequestPasswordResetAsync(string emailAddress, string? host = MailingErtisAuthInstance.Host)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		using var request = new HttpRequestMessage(HttpMethod.Post, $"{this.UsersUrl}/reset-password");
		request.Content = JsonContent.Create(new { email_address = emailAddress });
		
		if (host != null)
		{
			request.Headers.Add("X-Host", host);
		}
		
		return await adminClient.SendAsync(request, CancellationToken);
	}
	
	/// <summary>
	/// The link in the mail, and its reset token as a browser reads it from the query string.
	/// </summary>
	// ReSharper disable once UnusedTupleComponentInReturnValue
	private static (string Link, string ResetToken) ReadResetLink(MimeMessage message)
	{
		var link = HttpUtility.HtmlDecode(LinkRegex().Match(message.HtmlBody ?? string.Empty).Groups["link"].Value);
		Assert.StartsWith($"{MailingErtisAuthInstance.Host}?rpt=", link);
		
		var resetToken = HttpUtility.ParseQueryString(new Uri(link).Query)["rpt"];
		Assert.False(string.IsNullOrEmpty(resetToken));
		return (link, resetToken);
	}
	
	private async Task<HttpResponseMessage> VerifyResetTokenAsync(string resetToken)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.GetAsync($"{this.UsersUrl}/verify-reset-token?token={Uri.EscapeDataString(resetToken)}", CancellationToken);
	}
	
	private async Task<HttpResponseMessage> SetPasswordAsync(string resetToken, string emailAddress, string password)
	{
		var adminClient = await this._instance.CreateAdminClientAsync();
		return await adminClient.PostAsJsonAsync($"{this.UsersUrl}/set-password", new { reset_token = resetToken, email_address = emailAddress, password }, CancellationToken);
	}
	
	private async Task AssertSessionIsValidAsync((string AccessToken, string RefreshToken) session)
	{
		using var meResponse = await this._instance.CreateClient($"Bearer {session.AccessToken}").GetAsync("/me", CancellationToken);
		await ResourceClient.AssertStatusAsync(meResponse, HttpStatusCode.OK);
	}
	
	private async Task AssertSessionIsRevokedAsync((string AccessToken, string RefreshToken) session)
	{
		using var meResponse = await this._instance.CreateClient($"Bearer {session.AccessToken}").GetAsync("/me", CancellationToken);
		Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
		using var refreshResponse = await this._instance.CreateClient($"Bearer {session.RefreshToken}").GetAsync("/refresh-token", CancellationToken);
		Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
	}
	
	[GeneratedRegex("href=\"(?<link>[^\"]+)\"")]
	private static partial Regex LinkRegex();
	
	#endregion
	
	#region Change Password
	
	[Fact]
	public async Task ChangePassword_TheNewPasswordReplacesTheOldOne()
	{
		var (id, username, _) = await this.CreateUserAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PutAsJsonAsync($"{this.UsersUrl}/{id}/change-password", new { password = NewPassword }, CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		
		await this.AssertCanLoginAsync(username, NewPassword);
		await this.AssertCanNotLoginAsync(username, Password);
	}
	
	[Fact]
	public async Task ChangePassword_WithoutPassword_ReturnsBadRequest()
	{
		var (id, username, _) = await this.CreateUserAsync();
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PutAsJsonAsync($"{this.UsersUrl}/{id}/change-password", new { password = "" }, CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
		
		await this.AssertCanLoginAsync(username, Password);
	}
	
	[Fact]
	public async Task ChangePassword_ByTheUserItself()
	{
		var (id, username, _) = await this.CreateUserAsync();
		var (accessToken, _) = await this._instance.GenerateTokenAsync(username, Password);
		var userClient = this._instance.CreateClient($"Bearer {accessToken}");
		
		using var response = await userClient.PutAsJsonAsync($"{this.UsersUrl}/{id}/change-password", new { password = NewPassword }, CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		
		await this.AssertCanLoginAsync(username, NewPassword);
	}
	
	[Fact]
	public async Task ChangePassword_ByAnAdministrator_RevokesAllSessionsOfTheUser()
	{
		var (id, username, _) = await this.CreateUserAsync();
		var session = await this._instance.GenerateTokenAsync(username, Password);
		var adminClient = await this._instance.CreateAdminClientAsync();
		
		using var response = await adminClient.PutAsJsonAsync($"{this.UsersUrl}/{id}/change-password", new { password = NewPassword }, CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		
		await this.AssertSessionIsRevokedAsync(session);
	}
	
	[Fact]
	public async Task ChangePassword_ByTheUserItself_KeepsOnlyTheCallersSession()
	{
		var (id, username, _) = await this.CreateUserAsync();
		var callersSession = await this._instance.GenerateTokenAsync(username, Password);
		var otherSession = await this._instance.GenerateTokenAsync(username, Password);
		var userClient = this._instance.CreateClient($"Bearer {callersSession.AccessToken}");
		
		using var response = await userClient.PutAsJsonAsync($"{this.UsersUrl}/{id}/change-password", new { password = NewPassword }, CancellationToken);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.OK);
		
		await this.AssertSessionIsValidAsync(callersSession);
		await this.AssertSessionIsRevokedAsync(otherSession);
	}
	
	#endregion
	
	#region Check Password
	
	[Fact]
	public async Task CheckPassword_ChecksTheCallersPassword()
	{
		var (_, username, _) = await this.CreateUserAsync();
		var (accessToken, _) = await this._instance.GenerateTokenAsync(username, Password);
		var userClient = this._instance.CreateClient($"Bearer {accessToken}");
		
		using var correct = await userClient.GetAsync($"{this.UsersUrl}/check-password?password={Uri.EscapeDataString(Password)}", CancellationToken);
		await ResourceClient.AssertStatusAsync(correct, HttpStatusCode.OK);
		
		using var wrong = await userClient.GetAsync($"{this.UsersUrl}/check-password?password={Uri.EscapeDataString(NewPassword)}", CancellationToken);
		await ResourceClient.AssertStatusAsync(wrong, HttpStatusCode.Unauthorized);
	}
	
	#endregion
	
	#region Reset Password
	
	[Fact]
	public async Task ResetPassword_WithTheLinkFromTheMail_SetsTheNewPassword()
	{
		var (_, username, emailAddress) = await this.CreateUserAsync();
		var session = await this._instance.GenerateTokenAsync(username, Password);
		
		using var resetResponse = await this.RequestPasswordResetAsync(emailAddress);
		var reset = await ResourceClient.AssertStatusAsync(resetResponse, HttpStatusCode.OK);
		Assert.True(reset!["expiresIn"] != null, reset.ToJsonString());
		
		var mail = await this._instance.Smtp.WaitForMessageAsync(emailAddress, x => x.Subject == "Reset your password");
		var (_, resetToken) = ReadResetLink(mail);
		
		using var verifyResponse = await this.VerifyResetTokenAsync(resetToken);
		var verified = await ResourceClient.AssertStatusAsync(verifyResponse, HttpStatusCode.OK);
		Assert.Equal(emailAddress, verified!["email_address"]!.GetValue<string>());
		
		using var setResponse = await this.SetPasswordAsync(resetToken, emailAddress, NewPassword);
		await ResourceClient.AssertStatusAsync(setResponse, HttpStatusCode.OK);
		
		await this.AssertCanLoginAsync(username, NewPassword);
		await this.AssertCanNotLoginAsync(username, Password);
		await this.AssertSessionIsRevokedAsync(session);
		
		// Single use
		using var verifyAgainResponse = await this.VerifyResetTokenAsync(resetToken);
		Assert.Equal(HttpStatusCode.Unauthorized, verifyAgainResponse.StatusCode);
		using var setAgainResponse = await this.SetPasswordAsync(resetToken, emailAddress, "Another-P@ssw0rd!");
		Assert.Equal(HttpStatusCode.Unauthorized, setAgainResponse.StatusCode);
		await this.AssertCanLoginAsync(username, NewPassword);
	}
	
	[Fact]
	public async Task ResetPassword_SendsASingleMail()
	{
		var (_, _, emailAddress) = await this.CreateUserAsync();
		
		using var resetResponse = await this.RequestPasswordResetAsync(emailAddress);
		await ResourceClient.AssertStatusAsync(resetResponse, HttpStatusCode.OK);
		await this._instance.Smtp.WaitForMessageAsync(emailAddress);
		
		// Mails are sent in the background: give a second mail the time to arrive
		await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken);
		Assert.Single(this._instance.Smtp.MessagesTo(emailAddress));
	}
	
	[Fact]
	public async Task SetPassword_WithAnotherUsersResetToken_ReturnsUnauthorized()
	{
		var (_, _, emailAddress) = await this.CreateUserAsync();
		var (_, otherUsername, otherEmailAddress) = await this.CreateUserAsync();
		
		using var resetResponse = await this.RequestPasswordResetAsync(emailAddress);
		await ResourceClient.AssertStatusAsync(resetResponse, HttpStatusCode.OK);
		var (_, resetToken) = ReadResetLink(await this._instance.Smtp.WaitForMessageAsync(emailAddress));
		
		using var setResponse = await this.SetPasswordAsync(resetToken, otherEmailAddress, NewPassword);
		Assert.Equal(HttpStatusCode.Unauthorized, setResponse.StatusCode);
		await this.AssertCanLoginAsync(otherUsername, Password);
	}
	
	[Fact]
	public async Task ResetPassword_WithoutHost_ReturnsBadRequest()
	{
		var (_, _, emailAddress) = await this.CreateUserAsync();
		
		using var response = await this.RequestPasswordResetAsync(emailAddress, host: null);
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.BadRequest);
	}
	
	[Fact]
	public async Task ResetPassword_WithUnknownEmailAddress_ReturnsNotFound()
	{
		using var response = await this.RequestPasswordResetAsync($"unknown{Guid.NewGuid():N}@example.com");
		await ResourceClient.AssertStatusAsync(response, HttpStatusCode.NotFound);
	}
	
	#endregion
}