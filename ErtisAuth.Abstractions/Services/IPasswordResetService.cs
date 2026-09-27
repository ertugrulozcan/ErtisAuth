using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services;

/// <summary>
/// The forgot password flow: reset-password (token + mail), verify-reset-token and set-password.
/// </summary>
public interface IPasswordResetService
{
	Task<ResetPasswordToken> ResetPasswordAsync(Utilizer utilizer, string membershipId, string emailAddress, string host, CancellationToken cancellationToken = default);
	
	Task<ResetPasswordToken> GenerateResetPasswordTokenAsync(
		User user,
		Membership membership,
		bool asBase64 = false,
		ResetPasswordToken.ResetPasswordTokenPurpose purpose = ResetPasswordToken.ResetPasswordTokenPurpose.ResetPassword,
		CancellationToken cancellationToken = default);
	
	Task<User> VerifyResetTokenAsync(string membershipId, string resetToken, CancellationToken cancellationToken = default);
	
	Task SetPasswordAsync(Utilizer utilizer, string membershipId, string resetToken, string usernameOrEmailAddress, string password, CancellationToken cancellationToken = default);
}
