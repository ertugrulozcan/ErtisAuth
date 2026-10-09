using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services;

public interface IOneTimePasswordService : IMembershipBoundedService<OneTimePassword>
{
	/// <summary>
	/// Generates a one-time password for the user, replacing the previous ones. The plain code is returned only here;
	/// no reset token exists until the code is verified.
	/// </summary>
	Task<OneTimePassword> GenerateAsync(Utilizer utilizer, string membershipId, string userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Verifies the code and, when it is right, consumes the one-time password and generates the reset token.
	/// Returns null for a wrong code, an unknown user or a code that was already used.
	/// </summary>
	Task<ResetPasswordToken?> VerifyOtpAsync(string username, string password, string membershipId, string? host, CancellationToken cancellationToken = default);
}
