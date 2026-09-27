using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Infrastructure.Services;
using ErtisAuth.Infrastructure.Tests.Helpers;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// JwtService.ValidateActionTokenAsync: the checks shared by reset password and activation tokens.
/// </summary>
public class JwtServiceActionTokenTests
{
	#region Constants
	
	private const string UserId = "5f8a1b2c3d4e5f6a7b8c9d01";
	
	#endregion
	
	#region Fields
	
	private readonly JwtService _jwtService = new();
	
	#endregion
	
	#region Helpers
	
	private static User CreateUser(Membership membership)
	{
		return new User
		{
			Id = UserId,
			Username = "john.doe",
			EmailAddress = "john.doe@example.com",
			Role = "user",
			MembershipId = membership.Id
		};
	}
	
	private string CreateToken(
		Membership membership,
		string? tokenType = ActionTokens.ResetPasswordTokenType,
		string? signingSecretKey = null,
		string? principal = null,
		DateTime? generationTime = null)
	{
		var signingMembership = TestServiceFactory.CreateMembership("SHA2-256", membership.DefaultEncoding);
		signingMembership.Id = principal ?? membership.Id;
		signingMembership.Name = membership.Name;
		signingMembership.SecretKey = signingSecretKey ?? membership.SecretKey;
		
		var claims = new TokenClaims(Guid.NewGuid().ToString(), CreateUser(membership), signingMembership, TimeSpan.FromHours(1));
		if (tokenType != null)
		{
			claims.AddClaim(ActionTokens.TokenTypeClaim, tokenType);
		}
		
		return this._jwtService.GenerateToken(claims, generationTime: generationTime, encoding: membership.GetEncoding());
	}
	
	private async Task AssertRejectedAsync(string token, Membership membership, string errorCode = "InvalidToken")
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this._jwtService.ValidateActionTokenAsync(token, membership, ActionTokens.ResetPasswordTokenType));
		Assert.Equal(errorCode, exception.ErrorCode);
	}
	
	#endregion
	
	#region Tests
	
	[Theory]
	[InlineData(null)]
	[InlineData("utf-16")]
	public async Task ValidateActionTokenAsync_WithValidToken_ReturnsToken(string? defaultEncoding)
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256", defaultEncoding);
		var token = this.CreateToken(membership);
		
		var securityToken = await this._jwtService.ValidateActionTokenAsync(token, membership, ActionTokens.ResetPasswordTokenType);
		
		Assert.Equal(UserId, securityToken.Subject);
	}
	
	[Fact]
	public async Task ValidateActionTokenAsync_WithTokenSignedByAnotherKey_IsRejected()
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		
		await this.AssertRejectedAsync(this.CreateToken(membership, signingSecretKey: "attacker-secret-key-attacker-secret-key-attacker"), membership);
	}
	
	[Theory]
	[InlineData(ActionTokens.ActivationTokenType)]
	[InlineData(null)]
	public async Task ValidateActionTokenAsync_WithOtherOrNoTokenType_IsRejected(string? tokenType)
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		
		await this.AssertRejectedAsync(this.CreateToken(membership, tokenType), membership);
	}
	
	[Fact]
	public async Task ValidateActionTokenAsync_WithTokenOfAnotherMembership_IsRejected()
	{
		// Signed with the same key, but issued for another membership (prn claim)
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		
		await this.AssertRejectedAsync(this.CreateToken(membership, principal: "another-membership-id"), membership);
	}
	
	[Fact]
	public async Task ValidateActionTokenAsync_WithExpiredToken_IsRejectedAsExpired()
	{
		var membership = TestServiceFactory.CreateMembership("SHA2-256");
		
		await this.AssertRejectedAsync(this.CreateToken(membership, generationTime: DateTime.UtcNow.AddHours(-2)), membership, "TokenWasExpired");
	}
	
	[Theory]
	[InlineData("")]
	[InlineData("not-a-jwt")]
	public async Task ValidateActionTokenAsync_WithMalformedToken_IsRejected(string token)
	{
		await this.AssertRejectedAsync(token, TestServiceFactory.CreateMembership("SHA2-256"));
	}
	
	#endregion
}
