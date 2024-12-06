using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Abstractions.Services;

public interface IOneTimePasswordService : IMembershipBoundedService<OneTimePassword>
{
    Task<OneTimePassword> GenerateAsync(Utilizer utilizer, string membershipId, string userId, CancellationToken cancellationToken = default);

    Task ClearExpiredPasswordsAsync(string membershipId, CancellationToken cancellationToken = default);
    
    Task<OneTimePassword> VerifyOtpAsync(string username, string password, string membershipId, string host, CancellationToken cancellationToken = default);
    
    Task RevokeResetPasswordTokenAsync(Utilizer utilizer, string membershipId, string resetToken, CancellationToken cancellationToken = default);
}