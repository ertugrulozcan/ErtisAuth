using System.Text.Encodings.Web;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
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
	
	#endregion
	
	#region Fields
	
	private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
	
	#endregion
	
	#region Helpers
	
	private async Task<AuthenticateResult> AuthenticateAsync(string authorizationHeader)
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
			
		var context = new DefaultHttpContext();
		context.Request.Headers.Authorization = authorizationHeader;
		context.SetEndpoint(new RouteEndpoint(
			_ => Task.CompletedTask,
			RoutePatternFactory.Parse("/protected"),
			0,
			new EndpointMetadataCollection(new AuthorizedAttribute()),
			"protected"));
			
		await handler.InitializeAsync(new AuthenticationScheme(SchemeName, null, typeof(ErtisAuthAuthenticationHandler)), context);
		return await handler.AuthenticateAsync();
	}
	
	private void SetupBearerValidation(bool isRefreshToken)
	{
		var user = new User
		{
			Id = "user-id",
			Username = "john.doe",
			Role = string.Empty,
			IsActive = true,
			MembershipId = "membership-id"
		};
		
		this._tokenService
			.VerifyBearerTokenAsync(Token, false, Arg.Any<CancellationToken>())
			.Returns(new BearerTokenValidationResult(true, Token, user, TimeSpan.FromHours(1), isRefreshToken));
	}
	
	#endregion
	
	#region Tests
	
	[Fact]
	public async Task AuthenticateAsync_WithAccessToken_Succeeds()
	{
		this.SetupBearerValidation(isRefreshToken: false);
		
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
}
