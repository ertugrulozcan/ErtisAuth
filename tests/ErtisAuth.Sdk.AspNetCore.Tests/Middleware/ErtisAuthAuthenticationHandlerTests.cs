using System.Text.Encodings.Web;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Attributes;
using ErtisAuth.Extensions.Authorization.Extensions;
using ErtisAuth.Sdk.AspNetCore.Middleware;
using ErtisAuth.Sdk.AspNetCore.Models;
using ErtisAuth.Sdk.AspNetCore.Tests.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace ErtisAuth.Sdk.AspNetCore.Tests.Middleware;

/// <summary>
/// The authentication handler of client applications: which endpoints need authentication, which authorization handler
/// checks the token, and what the client gets when the check fails.
/// </summary>
public class ErtisAuthAuthenticationHandlerTests
{
	#region Constants
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	private const string ApplicationId = "5f8a1b2c3d4e5f6a7b8c9d02";
	
	#endregion
	
	#region Fields
	
	private readonly IAuthorizationHandler<BasicToken> _basicHandler = Substitute.For<IAuthorizationHandler<BasicToken>>();
	
	private readonly IAuthorizationHandler<BearerToken> _bearerHandler = Substitute.For<IAuthorizationHandler<BearerToken>>();
	
	#endregion
	
	#region Helpers
	
	private async Task<ErtisAuthAuthenticationHandler> CreateHandlerAsync(HttpContext httpContext)
	{
		var options = Substitute.For<IOptionsMonitor<AuthenticationSchemeOptions>>();
		options.Get(Arg.Any<string>()).Returns(new AuthenticationSchemeOptions());
		
		var handler = new ErtisAuthAuthenticationHandler(this._basicHandler, this._bearerHandler, options, NullLoggerFactory.Instance, UrlEncoder.Default);
		await handler.InitializeAsync(new AuthenticationScheme(ErtisAuth.Extensions.Authorization.Scheme.Name, null, typeof(ErtisAuthAuthenticationHandler)), httpContext);
		return handler;
	}
	
	private async Task<AuthenticateResult> AuthenticateAsync(HttpContext httpContext)
	{
		var handler = await this.CreateHandlerAsync(httpContext);
		return await handler.AuthenticateAsync();
	}
	
	/// <summary>
	/// What the pipeline does for a failed authentication: the ErtisAuth policy challenges this scheme.
	/// </summary>
	private async Task<AuthenticateResult> AuthenticateAndChallengeAsync(HttpContext httpContext)
	{
		var handler = await this.CreateHandlerAsync(httpContext);
		var result = await handler.AuthenticateAsync();
		await handler.ChallengeAsync(null);
		return result;
	}
	
	private static string ReadBody(HttpContext httpContext)
	{
		httpContext.Response.Body.Position = 0;
		return new StreamReader(httpContext.Response.Body).ReadToEnd();
	}
	
	private static Utilizer CreateUser()
	{
		return new Utilizer
		{
			Id = UserId,
			Username = "john.doe",
			Role = "user",
			Type = Utilizer.UtilizerType.User,
			MembershipId = "membership-id"
		};
	}
	
	private static AuthorizationResult Result(Utilizer utilizer, bool isAuthorized)
	{
		return new AuthorizationResult(utilizer, Rbac.Parse("users.read"), isAuthorized);
	}
	
	#endregion
	
	#region Endpoint Kinds
	
	[Fact]
	public async Task EndpointWithoutErtisAuthAttributes_IsNotAuthenticated()
	{
		var result = await this.AuthenticateAsync(TestHttpContext.Create("Bearer access-token"));
		
		Assert.True(result.None);
		await this._bearerHandler.DidNotReceiveWithAnyArgs().CheckAuthorizationAsync(null!, null!);
	}
	
	[Fact]
	public async Task UnauthorizedEndpoint_SucceedsWithPublicIdentityWithoutToken()
	{
		var result = await this.AuthenticateAsync(TestHttpContext.Create(null, new UnauthorizedAttribute()));
		
		Assert.True(result.Succeeded);
		Assert.Equal(ClaimExtensions.PublicClaimName, result.Principal!.Identities.Single().NameClaimType);
	}
	
	[Fact]
	public async Task AuthorizedEndpoint_WithPermittedBearerToken_SucceedsWithUtilizerIdentity()
	{
		this._bearerHandler.CheckAuthorizationAsync(Arg.Any<BearerToken>(), Arg.Any<HttpContext>()).Returns(Result(CreateUser(), true));
		
		var result = await this.AuthenticateAsync(TestHttpContext.Create("Bearer access-token", new AuthorizedAttribute()));
		
		Assert.True(result.Succeeded);
		Assert.Equal(ClaimExtensions.UtilizerClaimName, result.Principal!.Identities.Single().NameClaimType);
		await this._bearerHandler.Received(1).CheckAuthorizationAsync(Arg.Is<BearerToken>(x => x.AccessToken == "access-token"), Arg.Any<HttpContext>());
	}
	
	[Fact]
	public async Task AuthorizedEndpoint_WithBasicToken_UsesBasicHandler()
	{
		var application = new Utilizer { Id = ApplicationId, Username = "server-app", Role = "server", Type = Utilizer.UtilizerType.Application, MembershipId = "membership-id" };
		this._basicHandler.CheckAuthorizationAsync(Arg.Any<BasicToken>(), Arg.Any<HttpContext>()).Returns(Result(application, true));
		
		var result = await this.AuthenticateAsync(TestHttpContext.Create($"Basic {ApplicationId}:secret", new AuthorizedAttribute()));
		
		Assert.True(result.Succeeded);
		await this._basicHandler.Received(1).CheckAuthorizationAsync(Arg.Is<BasicToken>(x => x.AccessToken == $"{ApplicationId}:secret"), Arg.Any<HttpContext>());
		await this._bearerHandler.DidNotReceiveWithAnyArgs().CheckAuthorizationAsync(null!, null!);
	}
	
	[Fact]
	public async Task AuthorizedAndUnauthorizedEndpoint_IsPublic()
	{
		var result = await this.AuthenticateAsync(TestHttpContext.Create(null, new AuthorizedAttribute(), new UnauthorizedAttribute()));
		
		Assert.True(result.Succeeded);
		Assert.Equal(ClaimExtensions.PublicClaimName, result.Principal!.Identities.Single().NameClaimType);
	}
	
	[Fact]
	public async Task SelfAuthorizedEndpoint_AuthenticatesWithoutPermissionCheck()
	{
		this._bearerHandler.CheckAuthenticationAsync(Arg.Any<BearerToken>()).Returns(CreateUser());
		
		var result = await this.AuthenticateAsync(TestHttpContext.Create("Bearer access-token", new SelfAuthorizedAttribute()));
		
		Assert.True(result.Succeeded);
		await this._bearerHandler.Received(1).CheckAuthenticationAsync(Arg.Any<BearerToken>());
		await this._bearerHandler.DidNotReceiveWithAnyArgs().CheckAuthorizationAsync(null!, null!);
	}
	
	/// <summary>
	/// Endpoint metadata lists the controller's attributes first: a [SelfAuthorized] action of an [Authorized] controller.
	/// </summary>
	[Fact]
	public async Task SelfAuthorizedActionOfAuthorizedController_AuthenticatesWithoutPermissionCheck()
	{
		this._bearerHandler.CheckAuthenticationAsync(Arg.Any<BearerToken>()).Returns(CreateUser());
		
		var result = await this.AuthenticateAsync(TestHttpContext.Create("Bearer access-token", new AuthorizedAttribute(), new SelfAuthorizedAttribute()));
		
		Assert.True(result.Succeeded);
		await this._bearerHandler.Received(1).CheckAuthenticationAsync(Arg.Any<BearerToken>());
		await this._bearerHandler.DidNotReceiveWithAnyArgs().CheckAuthorizationAsync(null!, null!);
	}
	
	[Fact]
	public async Task SelfAuthorizedActionOfAuthorizedController_WithBasicToken_AuthenticatesWithoutPermissionCheck()
	{
		var application = new Utilizer { Id = ApplicationId, Username = "server-app", Role = "server", Type = Utilizer.UtilizerType.Application, MembershipId = "membership-id" };
		this._basicHandler.CheckAuthenticationAsync(Arg.Any<BasicToken>()).Returns(application);
		
		var result = await this.AuthenticateAsync(TestHttpContext.Create($"Basic {ApplicationId}:secret", new AuthorizedAttribute(), new SelfAuthorizedAttribute()));
		
		Assert.True(result.Succeeded);
		await this._basicHandler.Received(1).CheckAuthenticationAsync(Arg.Any<BasicToken>());
		await this._basicHandler.DidNotReceiveWithAnyArgs().CheckAuthorizationAsync(null!, null!);
	}
	
	[Fact]
	public async Task AuthorizedActionOfSelfAuthorizedController_ChecksThePermission()
	{
		this._bearerHandler.CheckAuthorizationAsync(Arg.Any<BearerToken>(), Arg.Any<HttpContext>()).Returns(Result(CreateUser(), true));
		
		var result = await this.AuthenticateAsync(TestHttpContext.Create("Bearer access-token", new SelfAuthorizedAttribute(), new AuthorizedAttribute()));
		
		Assert.True(result.Succeeded);
		await this._bearerHandler.Received(1).CheckAuthorizationAsync(Arg.Any<BearerToken>(), Arg.Any<HttpContext>());
		await this._bearerHandler.DidNotReceiveWithAnyArgs().CheckAuthenticationAsync(null!);
	}
	
	[Fact]
	public async Task SelfAuthorizedAndUnauthorizedEndpoint_IsPublic()
	{
		var result = await this.AuthenticateAsync(TestHttpContext.Create(null, new SelfAuthorizedAttribute(), new UnauthorizedAttribute()));

		Assert.True(result.Succeeded);
		Assert.Equal(ClaimExtensions.PublicClaimName, result.Principal!.Identities.Single().NameClaimType);
	}

	/// <summary>
	/// Endpoint metadata lists the controller's attributes first: a [SelfAuthorized] action of an [Unauthorized] controller.
	/// </summary>
	[Fact]
	public async Task SelfAuthorizedActionOfUnauthorizedController_AuthenticatesWithoutPermissionCheck()
	{
		this._bearerHandler.CheckAuthenticationAsync(Arg.Any<BearerToken>()).Returns(CreateUser());

		var result = await this.AuthenticateAsync(TestHttpContext.Create("Bearer access-token", new UnauthorizedAttribute(), new SelfAuthorizedAttribute()));

		Assert.True(result.Succeeded);
		Assert.Equal(ClaimExtensions.UtilizerClaimName, result.Principal!.Identities.Single().NameClaimType);
		await this._bearerHandler.Received(1).CheckAuthenticationAsync(Arg.Any<BearerToken>());
		await this._bearerHandler.DidNotReceiveWithAnyArgs().CheckAuthorizationAsync(null!, null!);
	}

	[Fact]
	public async Task SelfAuthorizedActionOfUnauthorizedController_WithoutToken_Fails()
	{
		var result = await this.AuthenticateAsync(TestHttpContext.Create(null, new UnauthorizedAttribute(), new SelfAuthorizedAttribute()));

		Assert.False(result.Succeeded);
	}

	#endregion
	
	#region Failures
	
	[Fact]
	public async Task AuthorizedEndpoint_WithBearerTokenNotPermitted_FailsWithForbidden()
	{
		this._bearerHandler.CheckAuthorizationAsync(Arg.Any<BearerToken>(), Arg.Any<HttpContext>()).Returns(Result(CreateUser(), false));
		var httpContext = TestHttpContext.Create("Bearer access-token", new AuthorizedAttribute());
		
		var result = await this.AuthenticateAndChallengeAsync(httpContext);
		
		Assert.NotNull(result.Failure);
		Assert.Contains("4032", result.Failure.Message);
		Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
	}
	
	[Fact]
	public async Task AuthorizedEndpoint_WithBasicTokenNotPermitted_FailsWithForbidden()
	{
		var application = new Utilizer { Id = ApplicationId, Username = "server-app", Role = "server", Type = Utilizer.UtilizerType.Application, MembershipId = "membership-id" };
		this._basicHandler.CheckAuthorizationAsync(Arg.Any<BasicToken>(), Arg.Any<HttpContext>()).Returns(Result(application, false));
		var httpContext = TestHttpContext.Create($"Basic {ApplicationId}:secret", new AuthorizedAttribute());
		
		var result = await this.AuthenticateAndChallengeAsync(httpContext);
		
		Assert.Contains("4031", result.Failure!.Message);
		Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
		Assert.False(httpContext.Response.Headers.ContainsKey("WWW-Authenticate"));
	}
	
	[Fact]
	public async Task AuthorizedEndpoint_WhenHandlerRejectsToken_FailsWithItsStatusCode()
	{
		this._bearerHandler.CheckAuthorizationAsync(Arg.Any<BearerToken>(), Arg.Any<HttpContext>()).Returns<AuthorizationResult>(_ => throw ErtisAuthException.Unauthorized("Token was expired"));
		var httpContext = TestHttpContext.Create("Bearer access-token", new AuthorizedAttribute());
		
		var result = await this.AuthenticateAndChallengeAsync(httpContext);
		
		Assert.Equal("Token was expired", result.Failure!.Message);
		Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
		Assert.Equal("application/json", httpContext.Response.ContentType);
		Assert.Equal(ErtisAuth.Extensions.Authorization.Scheme.WwwAuthenticate, httpContext.Response.Headers.WWWAuthenticate.ToString());
		Assert.Contains("Token was expired", ReadBody(httpContext));
	}
	
	[Fact]
	public async Task AuthorizedEndpoint_WithoutToken_ChallengesWithUnauthorizedAndWwwAuthenticate()
	{
		var httpContext = TestHttpContext.Create(null, new AuthorizedAttribute());
		
		await this.AuthenticateAndChallengeAsync(httpContext);
		
		Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
		Assert.Equal(ErtisAuth.Extensions.Authorization.Scheme.WwwAuthenticate, httpContext.Response.Headers.WWWAuthenticate.ToString());
		Assert.Contains("AuthorizationHeaderMissing", ReadBody(httpContext));
	}
	
	[Theory]
	[InlineData(null, "AuthorizationHeaderMissing")]
	[InlineData("Digest abc", "UnsupportedTokenType")]
	[InlineData("abc", "UnsupportedTokenType")]
	public async Task AuthorizedEndpoint_WithMissingOrUnsupportedToken_Fails(string? authorizationHeader, string errorCode)
	{
		var httpContext = TestHttpContext.Create(authorizationHeader, new AuthorizedAttribute());
		
		var result = await this.AuthenticateAsync(httpContext);
		
		Assert.NotNull(result.Failure);
		Assert.Equal(ErrorMessage(errorCode), result.Failure.Message);
		await this._bearerHandler.DidNotReceiveWithAnyArgs().CheckAuthorizationAsync(null!, null!);
		await this._basicHandler.DidNotReceiveWithAnyArgs().CheckAuthorizationAsync(null!, null!);
	}
	
	private static string ErrorMessage(string errorCode)
	{
		return errorCode switch
		{
			"AuthorizationHeaderMissing" => ErtisAuthException.AuthorizationHeaderMissing().Error.Message,
			_ => ErtisAuthException.UnsupportedTokenType().Error.Message
		};
	}
	
	#region Self Authorized Failures
	
	[Fact]
	public async Task SelfAuthorizedActionOfAuthorizedController_WithInvalidToken_FailsWithUnauthorized()
	{
		this._bearerHandler.CheckAuthenticationAsync(Arg.Any<BearerToken>()).Returns<Utilizer>(_ => throw ErtisAuthException.Unauthorized("Invalid token"));
		var httpContext = TestHttpContext.Create("Bearer access-token", new AuthorizedAttribute(), new SelfAuthorizedAttribute());
		
		var result = await this.AuthenticateAsync(httpContext);
		
		Assert.False(result.Succeeded);
	}
	
	[Fact]
	public async Task SelfAuthorizedActionOfAuthorizedController_WithoutToken_Fails()
	{
		var result = await this.AuthenticateAsync(TestHttpContext.Create(null, new AuthorizedAttribute(), new SelfAuthorizedAttribute()));
		
		Assert.False(result.Succeeded);
	}
	
	#endregion
	
	#endregion
}