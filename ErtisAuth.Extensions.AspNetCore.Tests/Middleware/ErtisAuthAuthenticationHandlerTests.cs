using System.Text.Encodings.Web;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.Extensions.AspNetCore.Middleware;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.Authorization.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ErtisAuth.Extensions.AspNetCore.Tests.Middleware;

public class ErtisAuthAuthenticationHandlerTests
{
	#region Constants
	
	private const string SchemeName = "ErtisAuth";
	
	private const string Token = "bearer-token";
	
	private const string BasicToken = "application-id:secret";
	
	private const string OwnMembershipId = "membership-a";
	
	private const string OtherMembershipId = "membership-b";
	
	#endregion
	
	#region Fields
	
	private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
	
	private readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	private readonly IAccessControlService _accessControlService = Substitute.For<IAccessControlService>();
	
	#endregion
	
	#region Helpers
	
	/// <param name="authorizationHeader"></param>
	/// <param name="routeMembershipId"></param>
	/// <param name="membershipRoute"></param>
	/// <param name="authorization">The endpoint's authorization attributes in metadata order (controller first, then action); [Authorized] by default</param>
	private async Task<AuthenticateResult> AuthenticateAsync(string authorizationHeader, string? routeMembershipId = null, bool membershipRoute = false, object[]? authorization = null)
	{
		var options = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
		options.Get(SchemeName).Returns(new AuthenticationSchemeOptions());
		
		var handler = new ErtisAuthAuthenticationHandler(
			options,
			this._tokenService,
			this._roleService,
			this._accessControlService,
			NullLoggerFactory.Instance,
			UrlEncoder.Default);
		
		var authorizationMetadata = authorization ?? [new AuthorizedAttribute()];
		var metadata = membershipRoute
			? new EndpointMetadataCollection(authorizationMetadata.Append(new MembershipRouteAttribute("users")))
			: new EndpointMetadataCollection(authorizationMetadata);
		
		var context = new DefaultHttpContext();
		context.Request.Headers.Authorization = authorizationHeader;
		context.SetEndpoint(new RouteEndpoint(
			_ => Task.CompletedTask,
			RoutePatternFactory.Parse(membershipRoute ? "memberships/{membershipId}/users" : "/protected"),
			0,
			metadata,
			"protected"));
		
		if (routeMembershipId != null)
		{
			context.Request.RouteValues[MembershipRouteAttribute.ParameterName] = routeMembershipId;
		}
		
		await handler.InitializeAsync(new AuthenticationScheme(SchemeName, null, typeof(ErtisAuthAuthenticationHandler)), context);
		return await handler.AuthenticateAsync();
	}
	
	private void SetupBearerValidation(bool isRefreshToken = false, string role = "")
	{
		var user = new User
		{
			Id = "user-id",
			Username = "john.doe",
			Role = role,
			IsActive = true,
			MembershipId = OwnMembershipId
		};
		
		this._tokenService
			.VerifyBearerTokenAsync(Token, false, Arg.Any<CancellationToken>())
			.Returns(new BearerTokenValidationResult(true, Token, user, TimeSpan.FromHours(1), isRefreshToken));
	}
	
	private void SetupBasicValidation()
	{
		var application = new Application
		{
			Id = "application-id",
			Name = "Test Application",
			Role = string.Empty,
			MembershipId = OwnMembershipId
		};
		
		this._tokenService
			.VerifyBasicTokenAsync(BasicToken, false, Arg.Any<CancellationToken>())
			.Returns(new BasicTokenValidationResult(true, BasicToken, application));
	}
	
	#endregion
	
	#region Refresh Tokens
	
	[Fact]
	public async Task AuthenticateAsync_WithAccessToken_Succeeds()
	{
		this.SetupBearerValidation();
		
		var result = await this.AuthenticateAsync($"Bearer {Token}");
		
		Assert.True(result.Succeeded);
	}
	
	[Fact]
	public async Task AuthenticateAsync_WithRefreshToken_Fails()
	{
		this.SetupBearerValidation(isRefreshToken: true);
		
		var result = await this.AuthenticateAsync($"Bearer {Token}");
		
		Assert.False(result.Succeeded);
		Assert.Equal("Refresh tokens can not be used as access tokens", result.Failure?.Message);
	}
	
	#endregion
	
	#region Membership Scope
	
	[Fact]
	public async Task AuthenticateAsync_WithBearerTokenOfSameMembership_Succeeds()
	{
		this.SetupBearerValidation();
		
		var result = await this.AuthenticateAsync($"Bearer {Token}", OwnMembershipId, membershipRoute: true);
		
		Assert.True(result.Succeeded);
	}
	
	[Fact]
	public async Task AuthenticateAsync_WithBearerTokenOfAnotherMembership_FailsWithAccessDenied()
	{
		this.SetupBearerValidation();
		
		var result = await this.AuthenticateAsync($"Bearer {Token}", OtherMembershipId, membershipRoute: true);
		
		Assert.False(result.Succeeded);
		Assert.Equal("You do not have access to the resources of this membership", result.Failure?.Message);
	}
	
	[Fact]
	public async Task AuthenticateAsync_WithBasicTokenOfSameMembership_Succeeds()
	{
		this.SetupBasicValidation();
		
		var result = await this.AuthenticateAsync($"Basic {BasicToken}", OwnMembershipId, membershipRoute: true);
		
		Assert.True(result.Succeeded);
	}
	
	[Fact]
	public async Task AuthenticateAsync_WithBasicTokenOfAnotherMembership_FailsWithAccessDenied()
	{
		this.SetupBasicValidation();
		
		var result = await this.AuthenticateAsync($"Basic {BasicToken}", OtherMembershipId, membershipRoute: true);
		
		Assert.False(result.Succeeded);
		Assert.Equal("You do not have access to the resources of this membership", result.Failure?.Message);
	}
	
	[Fact]
	public async Task AuthenticateAsync_OnMembershipRouteWithoutRouteValue_FailsWithAccessDenied()
	{
		this.SetupBearerValidation();
		
		var result = await this.AuthenticateAsync($"Bearer {Token}", routeMembershipId: null, membershipRoute: true);
		
		Assert.False(result.Succeeded);
	}
	
	[Fact]
	public async Task AuthenticateAsync_OnRouteWithoutMembershipRouteAttribute_IgnoresRouteMembershipId()
	{
		// e.g. MembershipsController (memberships/{id}) is intentionally not membership bounded.
		this.SetupBearerValidation();
		
		var result = await this.AuthenticateAsync($"Bearer {Token}", OtherMembershipId, membershipRoute: false);
		
		Assert.True(result.Succeeded);
	}
	
	#endregion
	
	#region Endpoint Authorization
	
	/// <summary>
	/// A user whose role denies every permission.
	/// </summary>
	private void SetupBearerValidationWithoutPermission()
	{
		this.SetupBearerValidation(role: "no-permission");
		this._roleService.GetBySlugAsync("no-permission", OwnMembershipId, Arg.Any<CancellationToken>()).Returns(new Role { Id = "role-id", Name = "No Permission", MembershipId = OwnMembershipId });
		this._accessControlService.HasPermission(Arg.Any<Role>(), Arg.Any<Rbac>(), Arg.Any<Utilizer>()).Returns(false);
	}
	
	[Fact]
	public async Task AuthorizedEndpoint_ChecksThePermission()
	{
		this.SetupBearerValidationWithoutPermission();
		
		var result = await this.AuthenticateAsync($"Bearer {Token}");
		
		Assert.False(result.Succeeded);
		this._accessControlService.ReceivedWithAnyArgs(1).HasPermission(default(Role)!, default(Rbac)!, default(Utilizer));
	}
	
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SelfAuthorizedEndpoint_AuthenticatesWithoutPermissionCheck(bool onActionOfAuthorizedController)
	{
		this.SetupBearerValidationWithoutPermission();
		object[] authorization = onActionOfAuthorizedController ? [new AuthorizedAttribute(), new SelfAuthorizedAttribute()] : [new SelfAuthorizedAttribute()];
		
		var result = await this.AuthenticateAsync($"Bearer {Token}", authorization: authorization);
		
		Assert.True(result.Succeeded);
		this._accessControlService.DidNotReceiveWithAnyArgs().HasPermission(default(Role)!, default(Rbac)!, default(Utilizer));
	}
	
	[Fact]
	public async Task AuthorizedActionOfSelfAuthorizedController_ChecksThePermission()
	{
		this.SetupBearerValidationWithoutPermission();
		
		var result = await this.AuthenticateAsync($"Bearer {Token}", authorization: [new SelfAuthorizedAttribute(), new AuthorizedAttribute()]);
		
		Assert.False(result.Succeeded);
	}
	
	[Fact]
	public async Task SelfAuthorizedEndpoint_StillRejectsRefreshTokens()
	{
		this.SetupBearerValidation(isRefreshToken: true);
		
		var result = await this.AuthenticateAsync($"Bearer {Token}", authorization: [new AuthorizedAttribute(), new SelfAuthorizedAttribute()]);
		
		Assert.False(result.Succeeded);
	}
	
	[Fact]
	public async Task SelfAuthorizedEndpoint_StillRejectsTokensOfAnotherMembership()
	{
		this.SetupBearerValidation();
		
		var result = await this.AuthenticateAsync($"Bearer {Token}", OtherMembershipId, membershipRoute: true, authorization: [new AuthorizedAttribute(), new SelfAuthorizedAttribute()]);
		
		Assert.False(result.Succeeded);
		Assert.Equal("You do not have access to the resources of this membership", result.Failure?.Message);
	}
	
	[Fact]
	public async Task SelfAuthorizedEndpoint_WithoutToken_Fails()
	{
		var result = await this.AuthenticateAsync(string.Empty, authorization: [new AuthorizedAttribute(), new SelfAuthorizedAttribute()]);
		
		Assert.False(result.Succeeded);
	}
	
	[Fact]
	public async Task UnauthorizedAttribute_MakesTheEndpointPublic()
	{
		var result = await this.AuthenticateAsync(string.Empty, authorization: [new SelfAuthorizedAttribute(), new UnauthorizedAttribute()]);
		
		Assert.True(result.Succeeded);
		Assert.Equal(ClaimExtensions.PublicClaimName, result.Principal!.Identities.Single().NameClaimType);
	}
	
	#endregion
	
	#region Infrastructure Errors
	
	/// <summary>
	/// An exception that is not an ErtisAuthException (e.g. the database is unreachable) is not an authentication failure:
	/// it goes to the global exception handler (500), so that clients don't take an outage for an invalid token (401).
	/// </summary>
	[Fact]
	public async Task AuthenticateAsync_WhenTheTokenCanNotBeVerified_Throws()
	{
		this._tokenService
			.VerifyBearerTokenAsync(Token, false, Arg.Any<CancellationToken>())
			.Returns<BearerTokenValidationResult>(_ => throw new TimeoutException("A timeout occurred after 30000ms selecting a server"));
		
		await Assert.ThrowsAsync<TimeoutException>(() => this.AuthenticateAsync($"Bearer {Token}"));
	}
	
	#endregion
}