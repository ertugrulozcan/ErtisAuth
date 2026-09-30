using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services;

/// <summary>
/// The forgot password flow: reset-password (token + mail), verify-reset-token and set-password.
/// </summary>
public interface IPasswordResetService
{
	Task<ResetPasswordToken> ResetPasswordAsync(string emailAddress, string membershipId, Utilizer utilizer, string host, CancellationToken cancellationToken = default);
	
	Task<ResetPasswordToken> GenerateResetPasswordTokenAsync(
		User user,
		Membership membership,
		bool asBase64 = false,
		ResetPasswordToken.ResetPasswordTokenPurpose purpose = ResetPasswordToken.ResetPasswordTokenPurpose.ResetPassword,
		CancellationToken cancellationToken = default);
	
	Task<User> VerifyResetTokenAsync(string resetToken, string membershipId, CancellationToken cancellationToken = default);
	
	Task<User> SetPasswordAsync(string usernameOrEmailAddress, string password, string resetToken, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
}
