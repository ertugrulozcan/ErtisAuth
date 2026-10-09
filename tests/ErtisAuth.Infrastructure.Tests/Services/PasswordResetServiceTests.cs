using System.Text;
using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// The forgot password flow: reset tokens must be signed with the membership key, of the reset type, bound to their user,
/// single use and only for active accounts.
/// </summary>
public class PasswordResetServiceTests : BaseActionTokenTests
{
	#region Helpers
	
	private async Task<string> GenerateResetLinkAsync(string userId, string username)
	{
		var token = await this.CreatePasswordResetService().GenerateResetPasswordTokenAsync(this.UserModel(userId, username), this._membership, asBase64: true, cancellationToken: TestContext.Current.CancellationToken);
		return token.Token;
	}
	
	private async Task SetPasswordAsync(string resetLink, string usernameOrEmailAddress)
	{
		await this.CreatePasswordResetService().SetPasswordAsync(usernameOrEmailAddress, NewPassword, resetLink, this._membership.Id, this.PublicPageApplication(), TestContext.Current.CancellationToken);
	}
	
	#endregion
	
	#region Reset Password
	
	[Fact]
	public async Task SetPasswordAsync_WithIssuedResetToken_ChangesPassword()
	{
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		
		await this.SetPasswordAsync(resetLink, "victim");
		
		Assert.NotNull(this._persistedDocument);
		Assert.NotEqual("victim-password-hash", this._persistedDocument["password_hash"].AsString);
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithTokenSignedByAnotherKey_IsRejected()
	{
		var forgedToken = this.CreateToken(VictimId, "victim", ActionTokens.ResetPasswordTokenType, "victim-password-hash", signingSecretKey: AttackerSecretKey);
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(forgedToken), "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithOwnTokenForAnotherUser_IsRejected()
	{
		// A genuine reset link of the attacker's own account, used to set the victim's password
		var attackerResetLink = await this.GenerateResetLinkAsync(AttackerId, "attacker");
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(attackerResetLink, "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithActivationToken_IsRejected()
	{
		var activationToken = this.CreateToken(VictimId, "victim", ActionTokens.ActivationTokenType);
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(activationToken), "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithTokenWithoutType_IsRejected()
	{
		// e.g. an access token of the victim
		var claims = new TokenClaims(Guid.NewGuid().ToString(), this.UserModel(VictimId, "victim"), this._membership, TimeSpan.FromHours(1));
		var accessToken = this._jwtService.GenerateToken(claims);
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(accessToken), "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithTokenOfAnotherMembership_IsRejected()
	{
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		var token = Encoding.UTF8.GetString(Convert.FromBase64String(resetLink)).Split(':', 2)[1];
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(token, "another-membership-id"), "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_AfterPasswordWasChanged_IsRejected()
	{
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		
		// The first use changes the password
		((IDictionary<string, object?>)this._users[VictimId])["password_hash"] = "changed-password-hash";
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(resetLink, "victim"));
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithExpiredToken_IsRejectedAsExpired()
	{
		var expiredToken = this.CreateToken(VictimId, "victim", ActionTokens.ResetPasswordTokenType, "victim-password-hash", generationTime: DateTime.UtcNow.AddHours(-2));
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(this.Link(expiredToken), "victim"), "TokenWasExpired");
	}
	
	[Fact]
	public async Task SetPasswordAsync_WithUtf16Membership_ChangesPassword()
	{
		this.SetupMembership("utf-16");
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		
		await this.SetPasswordAsync(resetLink, "victim");
		
		Assert.NotNull(this._persistedDocument);
	}
	
	[Fact]
	public async Task GenerateResetPasswordTokenAsync_ForInactiveUser_IsRejected()
	{
		var inactiveUser = this.UserModel(InactiveUserId, "newcomer");
		inactiveUser.IsActive = false;
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreatePasswordResetService().GenerateResetPasswordTokenAsync(inactiveUser, this._membership, cancellationToken: TestContext.Current.CancellationToken));
		
		Assert.Equal("UserInactive", exception.ErrorCode);
	}
	
	[Fact]
	public async Task SetPasswordAsync_ForUserFrozenAfterTokenWasIssued_IsRejected()
	{
		var resetLink = await this.GenerateResetLinkAsync(VictimId, "victim");
		((IDictionary<string, object?>)this._users[VictimId])["is_active"] = false;
		
		await this.AssertRejectedAsync(() => this.SetPasswordAsync(resetLink, "victim"), "UserInactive");
	}
	
	[Fact]
	public async Task VerifyResetTokenAsync_WithMalformedLink_IsRejected()
	{
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this.CreatePasswordResetService().VerifyResetTokenAsync("not-a-reset-link", this._membership.Id, TestContext.Current.CancellationToken));
		
		Assert.Equal("InvalidToken", exception.ErrorCode);
	}
	
	#endregion
}
