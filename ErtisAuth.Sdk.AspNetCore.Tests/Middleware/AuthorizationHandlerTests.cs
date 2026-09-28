using System.Net;
using System.Text.Json;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.AspNetCore.Middleware;
using ErtisAuth.Sdk.AspNetCore.Tests.Helpers;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace ErtisAuth.Sdk.AspNetCore.Tests.Middleware;

/// <summary>
/// Basic and bearer authorization handlers: they ask the ErtisAuth API who the utilizer is and whether it may perform the action
/// of the endpoint ([utilizer].users.read.user-1 for the test endpoint).
/// </summary>
public class AuthorizationHandlerTests
{
	#region Constants
	
	private const string ApplicationId = "5f8a1b2c3d4e5f6a7b8c9d02";
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	#endregion
	
	#region Fields
	
	private readonly IApplicationService _applicationService = Substitute.For<IApplicationService>();
	
	private readonly IAuthenticationService _authenticationService = Substitute.For<IAuthenticationService>();
	
	private readonly IRoleService _roleService = Substitute.For<IRoleService>();
	
	private readonly BasicToken _basicToken = new($"{ApplicationId}:secret");
	
	private readonly BearerToken _bearerToken = BearerToken.CreateTemp("access-token");
	
	#endregion
	
	#region Helpers
	
	private BasicAuthorizationHandler CreateBasicHandler(int? basicTokenCacheTTL = null)
	{
		return new BasicAuthorizationHandler(
			this._applicationService,
			this._roleService,
			new MemoryCache(new MemoryCacheOptions()),
			new ErtisAuthOptions { BaseUrl = "https://auth.test", MembershipId = "membership-id", BasicTokenCacheTTL = basicTokenCacheTTL },
			NullLogger<BasicAuthorizationHandler>.Instance);
	}
	
	private BearerAuthorizationHandler CreateBearerHandler()
	{
		return new BearerAuthorizationHandler(this._authenticationService, this._roleService, NullLogger<BearerAuthorizationHandler>.Instance);
	}
	
	private void SetupApplication()
	{
		this._applicationService.GetAsync(ApplicationId, this._basicToken, Arg.Any<CancellationToken>()).Returns(new ResponseResult<Application>(HttpStatusCode.OK, string.Empty)
		{
			Data = new Application { Id = ApplicationId, Name = "server-app", Role = "server", MembershipId = "membership-id" }
		});
	}
	
	private void SetupUser()
	{
		this._authenticationService.WhoAmIAsync(this._bearerToken, Arg.Any<CancellationToken>()).Returns(new ResponseResult<User>(HttpStatusCode.OK, string.Empty)
		{
			Data = new User { Id = UserId, Username = "john.doe", Role = "user", MembershipId = "membership-id" }
		});
	}
	
	private void SetupPermission(bool isPermitted)
	{
		this._roleService.CheckPermissionAsync(Arg.Any<string>(), Arg.Any<TokenBase>(), Arg.Any<CancellationToken>()).Returns(isPermitted);
	}
	
	/// <summary>
	/// An error response of the API, whose message is the serialized error model.
	/// </summary>
	private static ResponseResult<T> ErrorResponse<T>(string message)
	{
		return new ResponseResult<T>(HttpStatusCode.Unauthorized, JsonSerializer.Serialize(ErtisAuthException.InvalidToken(message).Error));
	}
	
	#endregion
	
	#region Basic
	
	[Fact]
	public async Task Basic_CheckAuthorizationAsync_WhenPermitted_ReturnsApplicationUtilizer()
	{
		this.SetupApplication();
		this.SetupPermission(true);
		
		var result = await this.CreateBasicHandler().CheckAuthorizationAsync(this._basicToken, TestHttpContext.Create());
		
		Assert.True(result.IsAuthorized);
		Assert.Equal(ApplicationId, result.Utilizer.Id);
		Assert.Equal(SupportedTokenTypes.Basic, result.Utilizer.TokenType);
		Assert.Equal($"{ApplicationId}.users.read.user-1", result.Rbac.ToString());
		await this._roleService.Received(1).CheckPermissionAsync($"{ApplicationId}.users.read.user-1", this._basicToken, Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task Basic_CheckAuthorizationAsync_WhenNotPermitted_ReturnsUnauthorizedResult()
	{
		this.SetupApplication();
		this.SetupPermission(false);
		
		var result = await this.CreateBasicHandler().CheckAuthorizationAsync(this._basicToken, TestHttpContext.Create());
		
		Assert.False(result.IsAuthorized);
	}
	
	[Fact]
	public async Task Basic_CheckAuthorizationAsync_WhenApiRejectsToken_ThrowsUnauthorizedWithApiMessage()
	{
		this._applicationService.GetAsync(ApplicationId, this._basicToken, Arg.Any<CancellationToken>()).Returns(ErrorResponse<Application>("Application secret is wrong"));
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateBasicHandler().CheckAuthorizationAsync(this._basicToken, TestHttpContext.Create()));
		
		Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
		Assert.Equal("Application secret is wrong", exception.Message);
	}
	
	[Fact]
	public async Task Basic_CheckAuthorizationAsync_WithCache_AsksApiOnce()
	{
		this.SetupApplication();
		this.SetupPermission(true);
		var handler = this.CreateBasicHandler(basicTokenCacheTTL: 60);
		
		await handler.CheckAuthorizationAsync(this._basicToken, TestHttpContext.Create());
		var result = await handler.CheckAuthorizationAsync(this._basicToken, TestHttpContext.Create());
		
		Assert.True(result.IsAuthorized);
		await this._applicationService.Received(1).GetAsync(ApplicationId, this._basicToken, Arg.Any<CancellationToken>());
		await this._roleService.Received(1).CheckPermissionAsync(Arg.Any<string>(), Arg.Any<TokenBase>(), Arg.Any<CancellationToken>());
	}
	
	[Theory]
	[InlineData(null)]
	[InlineData(0)]
	public async Task Basic_CheckAuthorizationAsync_WithoutCache_AsksApiEveryTime(int? basicTokenCacheTTL)
	{
		this.SetupApplication();
		this.SetupPermission(true);
		var handler = this.CreateBasicHandler(basicTokenCacheTTL);
		
		await handler.CheckAuthorizationAsync(this._basicToken, TestHttpContext.Create());
		await handler.CheckAuthorizationAsync(this._basicToken, TestHttpContext.Create());
		
		await this._roleService.Received(2).CheckPermissionAsync(Arg.Any<string>(), Arg.Any<TokenBase>(), Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task Basic_CheckAuthorizationAsync_WithCache_DoesNotReuseResultOfOtherAction()
	{
		this.SetupApplication();
		this.SetupPermission(true);
		var handler = this.CreateBasicHandler(basicTokenCacheTTL: 60);
		
		await handler.CheckAuthorizationAsync(this._basicToken, TestHttpContext.Create());
		var otherObject = TestHttpContext.Create();
		otherObject.Request.RouteValues["id"] = "user-2";
		await handler.CheckAuthorizationAsync(this._basicToken, otherObject);
		
		await this._roleService.Received(1).CheckPermissionAsync($"{ApplicationId}.users.read.user-2", this._basicToken, Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task Basic_CheckAuthenticationAsync_ReturnsApplicationWithoutPermissionCheck()
	{
		this.SetupApplication();
		
		var utilizer = await this.CreateBasicHandler().CheckAuthenticationAsync(this._basicToken);
		
		Assert.Equal(ApplicationId, utilizer.Id);
		Assert.Equal($"{ApplicationId}:secret", utilizer.Token);
		await this._roleService.DidNotReceiveWithAnyArgs().CheckPermissionAsync(null!, null!, TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Bearer
	
	[Fact]
	public async Task Bearer_CheckAuthorizationAsync_WhenPermitted_ReturnsUserUtilizer()
	{
		this.SetupUser();
		this.SetupPermission(true);
		
		var result = await this.CreateBearerHandler().CheckAuthorizationAsync(this._bearerToken, TestHttpContext.Create());
		
		Assert.True(result.IsAuthorized);
		Assert.Equal(UserId, result.Utilizer.Id);
		Assert.Equal(SupportedTokenTypes.Bearer, result.Utilizer.TokenType);
		await this._roleService.Received(1).CheckPermissionAsync($"{UserId}.users.read.user-1", this._bearerToken, Arg.Any<CancellationToken>());
	}
	
	[Fact]
	public async Task Bearer_CheckAuthorizationAsync_WhenNotPermitted_ThrowsAccessDenied()
	{
		this.SetupUser();
		this.SetupPermission(false);
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateBearerHandler().CheckAuthorizationAsync(this._bearerToken, TestHttpContext.Create()));
		
		Assert.Equal("AccessDenied", exception.ErrorCode);
		Assert.Contains("4033", exception.Message);
	}
	
	[Fact]
	public async Task Bearer_CheckAuthorizationAsync_WhenApiRejectsToken_ThrowsUnauthorizedWithApiMessage()
	{
		this._authenticationService.WhoAmIAsync(this._bearerToken, Arg.Any<CancellationToken>()).Returns(ErrorResponse<User>("Token was expired"));
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateBearerHandler().CheckAuthorizationAsync(this._bearerToken, TestHttpContext.Create()));
		
		Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
		Assert.Equal("Token was expired", exception.Message);
	}
	
	[Fact]
	public async Task Bearer_CheckAuthorizationAsync_WhenApiErrorIsNotParsable_ThrowsGenericUnauthorized()
	{
		this._authenticationService.WhoAmIAsync(this._bearerToken, Arg.Any<CancellationToken>()).Returns(new ResponseResult<User>(HttpStatusCode.BadGateway, string.Empty));
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreateBearerHandler().CheckAuthorizationAsync(this._bearerToken, TestHttpContext.Create()));
		
		Assert.Equal(HttpStatusCode.Unauthorized, exception.StatusCode);
	}
	
	[Fact]
	public async Task Bearer_CheckAuthenticationAsync_ReturnsUserWithoutPermissionCheck()
	{
		this.SetupUser();
		
		var utilizer = await this.CreateBearerHandler().CheckAuthenticationAsync(this._bearerToken);
		
		Assert.Equal(UserId, utilizer.Id);
		Assert.Equal("access-token", utilizer.Token);
		await this._roleService.DidNotReceiveWithAnyArgs().CheckPermissionAsync(null!, null!, TestContext.Current.CancellationToken);
	}
	
	#endregion
}
