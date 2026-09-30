using System.Text;
using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Infrastructure.Tests.Services;

/// <summary>
/// User activation links: activation tokens must be signed with the membership key, of the activation type,
/// and issued after the last change of the user (a frozen user can not be reactivated with an old link).
/// </summary>
public class UserServiceActivationTests : BaseActionTokenTests
{
	#region Activation
	
	private async Task<User?> ActivateAsync(string token)
	{
		return await this.CreateUserService().ActivateUserAsync(this.Link(token), this._membership.Id, this.PublicPageApplication(), TestContext.Current.CancellationToken);
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithIssuedActivationToken_ActivatesUser()
	{
		var activationToken = this.CreateToken(InactiveUserId, "newcomer", ActionTokens.ActivationTokenType);
		
		await this.ActivateAsync(activationToken);
		
		Assert.NotNull(this._persistedDocument);
		Assert.True(this._persistedDocument["is_active"].AsBoolean);
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithTokenSignedByAnotherKey_IsRejected()
	{
		var forgedToken = this.CreateToken(InactiveUserId, "newcomer", ActionTokens.ActivationTokenType, signingSecretKey: AttackerSecretKey);
		
		await this.AssertRejectedAsync(() => this.ActivateAsync(forgedToken));
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithResetToken_IsRejected()
	{
		var resetToken = this.CreateToken(InactiveUserId, "newcomer", ActionTokens.ResetPasswordTokenType, "newcomer-password-hash");
		
		await this.AssertRejectedAsync(() => this.ActivateAsync(resetToken));
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithTokenIssuedBeforeUserWasFrozen_IsRejected()
	{
		// The activation link was sent, then an administrator froze the user
		var issuedAt = DateTime.UtcNow.AddMinutes(-30);
		this.AddUser(InactiveUserId, "newcomer", isActive: false, passwordHash: "newcomer-password-hash", modifiedAt: DateTime.UtcNow.AddMinutes(-10));
		var activationToken = this.CreateToken(InactiveUserId, "newcomer", ActionTokens.ActivationTokenType, generationTime: issuedAt);
		
		await this.AssertRejectedAsync(() => this.ActivateAsync(activationToken));
	}
	
	[Fact]
	public async Task ActivateUserAsync_WithTokenIssuedAfterLastChange_ActivatesUser()
	{
		this.AddUser(InactiveUserId, "newcomer", isActive: false, passwordHash: "newcomer-password-hash", modifiedAt: DateTime.UtcNow.AddMinutes(-30));
		var activationToken = this.CreateToken(InactiveUserId, "newcomer", ActionTokens.ActivationTokenType, generationTime: DateTime.UtcNow.AddMinutes(-10));
		
		await this.ActivateAsync(activationToken);
		
		Assert.NotNull(this._persistedDocument);
		Assert.True(this._persistedDocument["is_active"].AsBoolean);
	}
	
	#endregion
}
