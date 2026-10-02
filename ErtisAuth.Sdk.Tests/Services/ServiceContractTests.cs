using System.Net;
using System.Text.Json.Nodes;
using System.Web;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Sdk.Services.Interfaces;
using ErtisAuth.Sdk.Tests.Helpers;

namespace ErtisAuth.Sdk.Tests.Services;

/// <summary>
/// The requests of the membership, password, role and user services.
/// </summary>
public class ServiceContractTests
{
	#region Constants
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	private const string MembershipsUrl = $"{SdkTestServices.BaseUrl}/memberships";
	
	private const string MembershipUrl = $"{MembershipsUrl}/{SdkTestServices.MembershipId}";
	
	private const string MembershipJson = """{"_id":"5f8a1b2c3d4e5f6a7b8c9d00","name":"Test","secret_key":"secret","expires_in":3600,"refresh_token_expires_in":7200}""";
	
	#endregion
	
	#region Fields
	
	private readonly SdkTestServices _sdk = new();
	
	private readonly BearerToken _token = BearerToken.CreateTemp("access-token");
	
	#endregion
	
	#region Helpers
	
	private RecordedRequest LastRequest => this._sdk.Handler.LastRequest;
	
	private System.Collections.Specialized.NameValueCollection LastQuery => HttpUtility.ParseQueryString(this.LastRequest.Query);
	
	private static Membership CreateMembership(string id = SdkTestServices.MembershipId)
	{
		return new Membership
		{
			Id = id,
			Name = "Test",
			SecretKey = "secret"
		};
	}
	
	#endregion
	
	#region Memberships
	
	[Fact]
	public async Task MembershipService_UsesMembershipsUrlsWithoutMembershipScope()
	{
		var membershipService = this._sdk.Get<IMembershipService>();
		this._sdk.Handler.ResponseJson = MembershipJson;
		
		await membershipService.CreateMembershipAsync(CreateMembership(), this._token, TestContext.Current.CancellationToken);
		await membershipService.GetMembershipAsync(SdkTestServices.MembershipId, this._token, TestContext.Current.CancellationToken);
		await membershipService.UpdateMembershipAsync(CreateMembership(), this._token, TestContext.Current.CancellationToken);
		
		var requests = this._sdk.Handler.Requests.Select(x => $"{x.Method} {x.Path}").ToArray();
		Assert.Equal(
			[
				$"POST {MembershipsUrl}",
				$"GET {MembershipUrl}",
				$"PUT {MembershipUrl}"
			],
			requests);
		Assert.All(this._sdk.Handler.Requests, x => Assert.Equal("Bearer access-token", x.Header("Authorization")));
	}
	
	[Fact]
	public async Task MembershipService_WithSearchKeyword_CallsSearchEndpoint()
	{
		this._sdk.Handler.ResponseJson = """{"items":[],"count":0}""";
		
		await this._sdk.Get<IMembershipService>().GetMembershipsAsync(this._token, limit: 5, searchKeyword: "test", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal($"{MembershipsUrl}/search", this.LastRequest.Path);
		Assert.Equal("test", this.LastQuery["keyword"]);
	}
	
	[Fact]
	public async Task MembershipService_QueryMembershipsAsync_PostsQuery()
	{
		this._sdk.Handler.ResponseJson = """{"items":[],"count":0}""";
		
		await this._sdk.Get<IMembershipService>().QueryMembershipsAsync(this._token, """{"where":{}}""", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Post, this.LastRequest.Method);
		Assert.Equal($"{MembershipsUrl}/_query", this.LastRequest.Path);
		Assert.Equal("""{"where":{}}""", this.LastRequest.Body);
	}
	
	[Fact]
	public async Task MembershipService_UpdateWithoutId_FailsWithoutSendingRequest()
	{
		var response = await this._sdk.Get<IMembershipService>().UpdateMembershipAsync(CreateMembership(id: string.Empty), this._token, TestContext.Current.CancellationToken);
		
		Assert.False(response.IsSuccess);
		Assert.Empty(this._sdk.Handler.Requests);
	}
	
	[Fact]
	public async Task MembershipService_DeleteWithNoContentResponse_Succeeds()
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.NoContent;
		this._sdk.Handler.ResponseJson = null;
		
		var response = await this._sdk.Get<IMembershipService>().DeleteMembershipAsync(SdkTestServices.MembershipId, this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Delete, this.LastRequest.Method);
		Assert.Equal(MembershipUrl, this.LastRequest.Path);
		Assert.True(response.IsSuccess);
	}
	
	#endregion
	
	#region Passwords
	
	[Fact]
	public async Task PasswordService_ChangePasswordAsync_PutsPassword()
	{
		await this._sdk.Get<IPasswordService>().ChangePasswordAsync(UserId, "N3wP@ssw0rd", this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Put, this.LastRequest.Method);
		Assert.Equal($"{MembershipUrl}/users/{UserId}/change-password", this.LastRequest.Path);
		Assert.Equal("""{"password":"N3wP@ssw0rd"}""", this.LastRequest.Body);
	}
	
	[Fact]
	public async Task PasswordService_ResetPasswordAsync_PostsEmailAddress()
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.BadRequest;
		
		await this._sdk.Get<IPasswordService>().ResetPasswordAsync("john.doe@example.com", "https://app.example.com/reset", this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Post, this.LastRequest.Method);
		Assert.Equal($"{MembershipUrl}/users/reset-password", this.LastRequest.Path);
		Assert.Equal("""{"email_address":"john.doe@example.com"}""", this.LastRequest.Body);
	}
	
	[Fact]
	public async Task PasswordService_ResetPasswordAsync_SendsHostInXHostHeader()
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.BadRequest;
		
		await this._sdk.Get<IPasswordService>().ResetPasswordAsync("john.doe@example.com", "https://app.example.com/reset", this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal("https://app.example.com/reset", this.LastRequest.Header("X-Host"));
	}
	
	[Fact]
	public async Task PasswordService_ResetPasswordAsync_WithSuccessfulResponse_Succeeds()
	{
		this._sdk.Handler.ResponseJson = """{"message":"Reset token generated","expiresIn":7200}""";
		
		var response = await this._sdk.Get<IPasswordService>().ResetPasswordAsync("john.doe@example.com", "https://app.example.com/reset", this._token, TestContext.Current.CancellationToken);
		
		Assert.True(response.IsSuccess);
	}
	
	[Fact]
	public async Task PasswordService_SetPasswordAsync_PostsResetTokenAndPassword()
	{
		await this._sdk.Get<IPasswordService>().SetPasswordAsync("john.doe@example.com", "N3wP@ssw0rd", "reset-link-token", this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Post, this.LastRequest.Method);
		Assert.Equal($"{MembershipUrl}/users/set-password", this.LastRequest.Path);
		Assert.Equal("""{"email_address":"john.doe@example.com","reset_token":"reset-link-token","password":"N3wP@ssw0rd"}""", this.LastRequest.Body);
	}
	
	#endregion
	
	#region Roles
	
	[Theory]
	[InlineData(HttpStatusCode.OK, true)]
	[InlineData(HttpStatusCode.Forbidden, false)]
	[InlineData(HttpStatusCode.Unauthorized, false)]
	public async Task RoleService_CheckPermissionAsync_ReturnsWhetherApiPermits(HttpStatusCode statusCode, bool expected)
	{
		this._sdk.Handler.ResponseStatusCode = statusCode;
		
		var isPermitted = await this._sdk.Get<IRoleService>().CheckPermissionAsync("users.read", this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal(expected, isPermitted);
		Assert.Equal(HttpMethod.Get, this.LastRequest.Method);
		Assert.Equal($"{MembershipUrl}/roles/check-permission", this.LastRequest.Path);
		Assert.Equal("users.read", this.LastQuery["permission"]);
	}
	
	/// <summary>
	/// ErtisAuth not answering is not a denial: the permission is unknown (503 in the client application).
	/// </summary>
	[Theory]
	[InlineData(HttpStatusCode.InternalServerError)]
	[InlineData(HttpStatusCode.BadGateway)]
	[InlineData(HttpStatusCode.ServiceUnavailable)]
	public async Task RoleService_CheckPermissionAsync_WhenErtisAuthFails_ThrowsServiceUnavailable(HttpStatusCode statusCode)
	{
		this._sdk.Handler.ResponseStatusCode = statusCode;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this._sdk.Get<IRoleService>().CheckPermissionAsync("users.read", this._token, TestContext.Current.CancellationToken));
		
		Assert.Equal("AuthenticationServiceUnavailable", exception.ErrorCode);
		Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
	}
	
	/// <summary>
	/// The rest handler lets the network error through: not a denial either (the authentication handler of
	/// ErtisAuth.Sdk.AspNetCore turns it into 503).
	/// </summary>
	[Fact]
	public async Task RoleService_CheckPermissionAsync_WhenErtisAuthIsUnreachable_Throws()
	{
		this._sdk.Handler.ResponseException = new HttpRequestException("Connection refused");
		
		await Assert.ThrowsAsync<HttpRequestException>(() => this._sdk.Get<IRoleService>().CheckPermissionByRoleAsync("role-id", "users.read", this._token, TestContext.Current.CancellationToken));
	}
	
	[Fact]
	public async Task RoleService_CheckPermissionByRoleAsync_UsesRoleUrl()
	{
		await this._sdk.Get<IRoleService>().CheckPermissionByRoleAsync("role-id", "users.read", this._token, TestContext.Current.CancellationToken);
		
		Assert.Equal($"{MembershipUrl}/roles/role-id/check-permission", this.LastRequest.Path);
		Assert.Equal("users.read", this.LastQuery["permission"]);
	}
	
	#endregion
	
	#region Users
	
	[Fact]
	public async Task UserService_GetActiveTokensAsync_QueriesTokensOfUser()
	{
		this._sdk.Handler.ResponseJson = """{"items":[],"count":0}""";
		
		await this._sdk.Get<IUserService>().GetActiveTokensAsync(UserId, this._token, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Post, this.LastRequest.Method);
		Assert.Equal($"{MembershipUrl}/active-tokens/_query", this.LastRequest.Path);
		var where = JsonNode.Parse(this.LastRequest.Body!)!["where"]!;
		Assert.Equal(UserId, where["user_id"]!.GetValue<string>());
		Assert.Equal(SdkTestServices.MembershipId, where["membership_id"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task UserService_GetRevokedTokensAsync_QueriesTokensOfUser()
	{
		this._sdk.Handler.ResponseJson = """{"items":[],"count":0}""";
		
		await this._sdk.Get<IUserService>().GetRevokedTokensAsync(UserId, this._token, cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal($"{MembershipUrl}/revoked-tokens/_query", this.LastRequest.Path);
		var where = JsonNode.Parse(this.LastRequest.Body!)!["where"]!;
		Assert.Equal(UserId, where["user_id"]!.GetValue<string>());
		Assert.Equal("bearer_token", where["token_type"]!.GetValue<string>());
	}
	
	[Fact]
	public async Task UserService_GetAsyncWithKeyword_ListsUsersWithKeywordQuery()
	{
		this._sdk.Handler.ResponseJson = """{"items":[],"count":0}""";
		
		await this._sdk.Get<IUserService>().GetAsync<object>(this._token, limit: 5, searchKeyword: "john", cancellationToken: TestContext.Current.CancellationToken);
		
		// Unlike the other services, users are searched through the list endpoint
		Assert.Equal($"{MembershipUrl}/users", this.LastRequest.Path);
		Assert.Equal("john", this.LastQuery["keyword"]);
		Assert.Equal("5", this.LastQuery["limit"]);
	}
	
	#endregion
}