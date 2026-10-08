using System.Security.Claims;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.Authorization.Extensions;
using ErtisAuth.Sdk.AspNetCore.Extensions;
using ErtisAuth.Sdk.AspNetCore.Middleware;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Sdk.AspNetCore.Tests.Extensions;

/// <summary>
/// Helpers the controllers of client applications use to read the token and the utilizer,
/// and the authorization handler of the ErtisAuth policy.
/// </summary>
public class ControllerExtensionsTests
{
	#region Helpers
	
	private static TestController CreateController(string? authorizationHeader = null, ClaimsPrincipal? user = null)
	{
		var httpContext = new DefaultHttpContext();
		if (authorizationHeader != null)
		{
			httpContext.Request.Headers.Authorization = authorizationHeader;
		}
		
		if (user != null)
		{
			httpContext.User = user;
		}
		
		return new TestController { ControllerContext = new ControllerContext { HttpContext = httpContext } };
	}
	
	/// <summary>
	/// A token with the claims of an ErtisAuth access token; the signature is not checked by these helpers.
	/// </summary>
	private static string CreateJwt(string userId, string username, string membershipId)
	{
		return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
		{
			Subject = new ClaimsIdentity(
			[
				new Claim("sub", userId),
				new Claim("unique_name", username),
				new Claim("prn", membershipId)
			]),
			SigningCredentials = new SigningCredentials(new SymmetricSecurityKey("test-secret-key-test-secret-key-test-secret-key"u8.ToArray()), SecurityAlgorithms.HmacSha256)
		});
	}
	
	/// <summary>
	/// As the authentication handler builds it (the token is part of the identity)
	/// </summary>
	private static ClaimsPrincipal CreateAuthenticatedUser()
	{
		var utilizer = new Utilizer { Id = "user-1", Username = "john.doe", Role = "user", Type = Utilizer.UtilizerType.User, MembershipId = "membership-id", Token = "access-token", TokenType = SupportedTokenTypes.Bearer };
		return new ClaimsPrincipal(utilizer.ToClaimsIdentity());
	}
	
	#endregion
	
	#region Authorization Header
	
	[Fact]
	public void GetTokenFromHeader_ReturnsTokenAndType()
	{
		var controller = CreateController("Bearer access-token");
		
		var token = controller.GetTokenFromHeader(out var tokenType);
		
		Assert.Equal("access-token", token);
		Assert.Equal("Bearer", tokenType);
		Assert.Equal("Bearer access-token", controller.GetAuthorizationHeader());
		Assert.Equal("Bearer access-token", controller.Request.GetAuthorizationHeader());
	}
	
	[Fact]
	public void GetTokenFromHeader_WithoutHeader_ReturnsNull()
	{
		var controller = CreateController();
		
		Assert.Null(controller.GetAuthorizationHeader());
		Assert.Null(controller.GetTokenFromHeader(out var tokenType));
		Assert.Null(tokenType);
	}
	
	#endregion
	
	#region Utilizer
	
	[Fact]
	public void GetUtilizer_FromUtilizerIdentity_ReturnsIt()
	{
		var result = CreateController(user: CreateAuthenticatedUser()).GetUtilizer();
		
		Assert.Equal("user-1", result?.Id);
		Assert.Equal("john.doe", result?.Username);
	}
	
	/// <summary>
	/// Regression: GetUtilizer read the token of a request which was not authenticated without verifying it,
	/// so anyone could send a self-made token claiming any identity
	/// </summary>
	[Theory]
	[InlineData("Bearer")]
	[InlineData("Basic")]
	[InlineData(null)]
	public void GetUtilizer_WithoutUtilizerIdentity_ReturnsNull(string? tokenType)
	{
		var authorizationHeader = tokenType switch
		{
			"Bearer" => $"Bearer {CreateJwt("user-1", "john.doe", "membership-id")}",
			"Basic" => "Basic app-1:wrong-secret",
			_ => null
		};
		
		Assert.Null(CreateController(authorizationHeader).GetUtilizer());
	}
	
	[Fact]
	public void GetUtilizer_OnPublicEndpoint_ReturnsNull()
	{
		var publicUser = new ClaimsPrincipal(new ClaimsIdentity([], null, ClaimExtensions.PublicClaimName, null));
		
		Assert.Null(CreateController($"Bearer {CreateJwt("user-1", "john.doe", "membership-id")}", publicUser).GetUtilizer());
	}
	
	[Fact]
	public void GetUnverifiedUtilizer_FromUtilizerIdentity_ReturnsIt()
	{
		// The verified identity wins over the claims of the token
		var result = CreateController($"Bearer {CreateJwt("user-2", "jane.doe", "membership-id")}", CreateAuthenticatedUser()).GetUnverifiedUtilizer();
		
		Assert.Equal("user-1", result?.Id);
	}
	
	[Fact]
	public void GetUnverifiedUtilizer_ByBasicToken_ReturnsApplication()
	{
		var result = CreateController("Basic app-1:secret").GetUnverifiedUtilizer();
		
		Assert.Equal("app-1", result?.Id);
		Assert.Equal(Utilizer.UtilizerType.Application, result?.Type);
		Assert.Equal(SupportedTokenTypes.Basic, result?.TokenType);
	}
	
	[Fact]
	public void GetUnverifiedUtilizer_ByBearerToken_ReadsTokenClaims()
	{
		var result = CreateController($"Bearer {CreateJwt("user-1", "john.doe", "membership-id")}").GetUnverifiedUtilizer();
		
		Assert.Equal("user-1", result?.Id);
		Assert.Equal("john.doe", result?.Username);
		Assert.Equal("membership-id", result?.MembershipId);
		Assert.Equal(SupportedTokenTypes.Bearer, result?.TokenType);
	}
	
	[Fact]
	public void GetUnverifiedUtilizer_WithoutToken_ThrowsInvalidToken()
	{
		var exception = Assert.Throws<ErtisAuthException>(() => CreateController().GetUnverifiedUtilizer());
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	[Fact]
	public void GetUnverifiedUtilizer_WithTokenWithoutType_ThrowsUnsupportedTokenType()
	{
		var exception = Assert.Throws<ErtisAuthException>(() => CreateController("access-token").GetUnverifiedUtilizer());
		
		Assert.Equal("TokenTypeNotSupported", exception.ErrorCode);
	}
	
	#endregion
	
	#region Error Results
	
	[Fact]
	public void ErrorResults_ReturnErtisAuthErrors()
	{
		var controller = CreateController();
		
		Assert.Equal(StatusCodes.Status401Unauthorized, controller.AuthorizationHeaderMissing().StatusCode);
		Assert.Equal(StatusCodes.Status401Unauthorized, controller.InvalidToken().StatusCode);
	}
	
	#endregion
	
	#region Authorization Policy
	
	[Theory]
	[InlineData(ClaimExtensions.UtilizerClaimName, true)]
	[InlineData(ClaimExtensions.PublicClaimName, true)]
	[InlineData("OtherScheme", false)]
	public async Task ErtisAuthAuthorizationHandler_SucceedsOnlyForErtisAuthIdentities(string nameClaimType, bool expected)
	{
		var requirement = new ErtisAuthAuthorizationRequirement();
		var user = new ClaimsPrincipal(new ClaimsIdentity([], "test", nameClaimType, null));
		var context = new AuthorizationHandlerContext([requirement], user, null);
		
		await new ErtisAuthAuthorizationHandler().HandleAsync(context);
		
		Assert.Equal(expected, context.HasSucceeded);
	}
	
	#endregion
	
	#region Helper Classes
	
	private sealed class TestController : ControllerBase;
	
	#endregion
}
