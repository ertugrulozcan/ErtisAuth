using System.Net;
using System.Security.Cryptography;
using System.Text;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Constants;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Infrastructure.Helpers;

namespace ErtisAuth.Infrastructure.Services;

/// <summary>
/// The forgot password flow. A reset token is issued for an active account and mailed as a link (?rpt=) to the host
/// given by the caller; the page verifies the token (verify-reset-token) and then sets the new password with it (set-password).
/// A reset token is signed with the membership key, bound to its user and to the current password (single use).
/// </summary>
public class PasswordResetService : IPasswordResetService
{
	#region Services
	
	private readonly IUserService _userService;
	private readonly IMembershipService _membershipService;
	private readonly IJwtService _jwtService;
	private readonly IMailHookService _mailHookService;
	private readonly IEventService _eventService;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="userService"></param>
	/// <param name="membershipService"></param>
	/// <param name="jwtService"></param>
	/// <param name="mailHookService"></param>
	/// <param name="eventService"></param>
	public PasswordResetService(
		IUserService userService,
		IMembershipService membershipService,
		IJwtService jwtService,
		IMailHookService mailHookService,
		IEventService eventService)
	{
		this._userService = userService;
		this._membershipService = membershipService;
		this._jwtService = jwtService;
		this._mailHookService = mailHookService;
		this._eventService = eventService;
	}
	
	#endregion
	
	#region Reset Password
	
	public async Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string emailAddress, string host, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(emailAddress))
		{
			throw ErtisAuthException.Synthetic(HttpStatusCode.BadRequest, "Email address required (email_address)", "UsernameOrEmailAddressRequired");
		}
		
		var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
		var user = await this._userService.GetByUsernameOrEmailAddressAsync(membershipId, emailAddress);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(emailAddress, "email_address");
		}
		
		var resetPasswordToken = await this.GenerateResetPasswordTokenAsync(user, membership, cancellationToken: cancellationToken);
		var resetPasswordLink = GenerateResetPasswordLink(resetPasswordToken, membershipId, host);
		
		var eventPayload = new
		{
			resetPasswordToken.Token,
			resetPasswordLink,
			user,
			membership
		};
		
		await this._eventService.FireEventAsync(ErtisAuthEventType.UserPasswordReset, user, membershipId, eventPayload, cancellationToken: cancellationToken);
		
		await this.SendResetPasswordMailAsync(resetPasswordToken, membership, user, host, cancellationToken: cancellationToken);
		
		return resetPasswordToken;
	}
	
	private async Task SendResetPasswordMailAsync(ResetPasswordToken resetPasswordToken, Membership membership, User user, string? host = null, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(host))
		{
			throw ErtisAuthException.HostRequired();
		}
		
		if (membership.MailProviders == null || !membership.MailProviders.Any())
		{
			throw ErtisAuthException.NotDefinedAnyMailProvider();
		}
		
		var resetPasswordMailHook = await this._mailHookService.GetResetPasswordMailHookAsync(membership.Id, cancellationToken: cancellationToken);
		if (resetPasswordMailHook == null)
		{
			throw ErtisAuthException.ResetPasswordMailHookWasNotDefined();
		}
		
		var resetPasswordLink = GenerateResetPasswordLink(resetPasswordToken, membership.Id, host);
		this._mailHookService.SendHookMailAsync(resetPasswordMailHook, user.Id, membership.Id, new
		{
			user,
			resetPasswordLink
		}, cancellationToken: cancellationToken);
	}
	
	private static string GenerateResetPasswordLink(ResetPasswordToken resetPasswordToken, string membershipId, string host)
	{
		return ActionTokenLinkHelper.GenerateLink(host, ActionTokenLinkHelper.ResetPasswordQueryParameter, membershipId, resetPasswordToken.Token);
	}
	
	#endregion
	
	#region Reset Token
	
	public async Task<ResetPasswordToken> GenerateResetPasswordTokenAsync(
		User user,
		Membership membership,
		bool asBase64 = false,
		ResetPasswordToken.ResetPasswordTokenPurpose purpose = ResetPasswordToken.ResetPasswordTokenPurpose.ResetPassword,
		CancellationToken cancellationToken = default)
	{
		// Reset tokens are only issued for active accounts (inactive or frozen accounts can not recover their password)
		if (!user.IsActive)
		{
			throw ErtisAuthException.UserInactive(user.Id);
		}
		
		var resetPasswordTokenTTL = TTLs.RESET_PASSWORD_TOKEN_TTL;
		if (purpose == ResetPasswordToken.ResetPasswordTokenPurpose.OneTimePassword)
		{
			if (membership.OtpSettings?.Policy?.ExpiresIn != null && membership.OtpSettings.Policy is { ExpiresIn: > 0 })
			{
				resetPasswordTokenTTL = TimeSpan.FromSeconds(membership.OtpSettings.Policy.ExpiresIn.Value);
			}
		}
		else
		{
			if (membership is { ResetPasswordTokenExpiresIn: > 0 })
			{
				resetPasswordTokenTTL = TimeSpan.FromSeconds(membership.ResetPasswordTokenExpiresIn.Value);
			}
		}
		
		// The token is bound to the current password, so that it can be used only once (using it changes the password)
		var passwordHash = user is UserWithPasswordHash userWithPasswordHash
			? userWithPasswordHash.PasswordHash
			: (await this._userService.GetUserWithPasswordAsync(membership.Id, user.Id, cancellationToken: cancellationToken))?.PasswordHash;
		
		var tokenClaims = new TokenClaims(Guid.NewGuid().ToString(), user, membership, resetPasswordTokenTTL);
		tokenClaims.AddClaim(ActionTokens.TokenTypeClaim, ActionTokens.ResetPasswordTokenType);
		tokenClaims.AddClaim(ActionTokens.PasswordFingerprintClaim, GetPasswordFingerprint(passwordHash));
		
		var resetToken = this._jwtService.GenerateToken(tokenClaims, encoding: membership.GetEncoding());
		if (asBase64)
		{
			resetToken = ActionTokenLinkHelper.Encode(membership.Id, resetToken);
		}
		
		return new ResetPasswordToken(resetToken, resetPasswordTokenTTL);
	}
	
	public async Task<User> VerifyResetTokenAsync(string membershipId, string resetToken, CancellationToken cancellationToken = default)
	{
		var membership = await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
		var token = ActionTokenLinkHelper.Decode(membershipId, resetToken) ?? throw ErtisAuthException.InvalidToken();
		var securityToken = await this._jwtService.ValidateActionTokenAsync(token, membership, ActionTokens.ResetPasswordTokenType);
		
		var user = await this._userService.GetUserWithPasswordAsync(membershipId, securityToken.Subject, cancellationToken: cancellationToken);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(securityToken.Subject, "_id");
		}
		
		// The account may have been frozen after the token was issued
		if (!user.IsActive)
		{
			throw ErtisAuthException.UserInactive(user.Id);
		}
		
		// Single use: the token is only valid for the password it was issued for
		var fingerprint = securityToken.Claims.FirstOrDefault(x => x.Type == ActionTokens.PasswordFingerprintClaim)?.Value;
		var currentFingerprint = GetPasswordFingerprint(user.PasswordHash);
		if (fingerprint == null || !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(fingerprint), Encoding.UTF8.GetBytes(currentFingerprint)))
		{
			throw ErtisAuthException.InvalidToken("Reset token was already used or the password has been changed");
		}
		
		return user;
	}
	
	private static string GetPasswordFingerprint(string? passwordHash)
	{
		return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(passwordHash ?? string.Empty)))[..32];
	}
	
	#endregion
	
	#region Set Password
	
	public async Task<User> SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(usernameOrEmailAddress))
		{
			throw ErtisAuthException.ValidationError(new []
			{
				"Username or email required!"
			});
		}
		
		await this.CheckMembershipAsync(membershipId, cancellationToken: cancellationToken);
		
		var tokenOwner = await this.VerifyResetTokenAsync(membershipId, resetToken, cancellationToken: cancellationToken);
		
		var user = await this._userService.GetUserWithPasswordAsync(membershipId, usernameOrEmailAddress, usernameOrEmailAddress, cancellationToken: cancellationToken);
		if (user == null)
		{
			throw ErtisAuthException.UserNotFound(usernameOrEmailAddress, "username or email_address");
		}
		
		// The reset token only allows setting the password of the user it was issued to
		if (user.Id != tokenOwner.Id)
		{
			throw ErtisAuthException.InvalidToken();
		}
		
		await this._userService.ChangePasswordAsync(utilizer, membershipId, user.Id, password, cancellationToken: cancellationToken);
		return user;
	}
	
	#endregion
	
	#region Membership Methods
	
	private async Task<Membership> CheckMembershipAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		var membership = await this._membershipService.GetAsync(membershipId, cancellationToken: cancellationToken);
		if (membership == null)
		{
			throw ErtisAuthException.MembershipNotFound(membershipId);
		}
		
		return membership;
	}
	
	#endregion
}
