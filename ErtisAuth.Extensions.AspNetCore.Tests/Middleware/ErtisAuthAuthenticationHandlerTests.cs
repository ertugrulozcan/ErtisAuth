using System.Text.Encodings.Web;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Extensions.AspNetCore.Attributes;
using ErtisAuth.Extensions.AspNetCore.Middleware;
using ErtisAuth.Extensions.Authorization.Attributes;
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
	
	#endregion
	
	#region Helpers
	
	private async Task<AuthenticateResult> AuthenticateAsync(string authorizationHeader, string? routeMembershipId = null, bool membershipRoute = false)
	{
		var options = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
		options.Get(SchemeName).Returns(new AuthenticationSchemeOptions());
		
		var handler = new ErtisAuthAuthenticationHandler(
			options,
			this._tokenService,
			Substitute.For<IRoleService>(),
			Substitute.For<IAccessControlService>(),
			NullLoggerFactory.Instance,
			UrlEncoder.Default);
			
		var metadata = membershipRoute
			? new EndpointMetadataCollection(new AuthorizedAttribute(), new MembershipRouteAttribute("users"))
			: new EndpointMetadataCollection(new AuthorizedAttribute());
			
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
	
	private void SetupBearerValidation(bool isRefreshToken = false)
	{
		var user = new User
		{
			Id = "user-id",
			Username = "john.doe",
			Role = string.Empty,
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
		Assert.Equal("The token does not belong to the membership of the requested resource", result.Failure?.Message);
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
		Assert.Equal("The token does not belong to the membership of the requested resource", result.Failure?.Message);
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
}
