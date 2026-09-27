using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Sdk.Services.Interfaces;

[ServiceLifetime(ServiceLifetime.Singleton)]
public interface IPasswordService
{
	Task<IResponseResult> ChangePasswordAsync(string userId, string newPassword, TokenBase token, CancellationToken cancellationToken = default);
	
	Task<IResponseResult<ResetPasswordToken>> ResetPasswordAsync(string emailAddress, string server, string host, TokenBase token, CancellationToken cancellationToken = default);
	
	Task<IResponseResult> SetPasswordAsync(string email, string password, string resetToken, TokenBase token, CancellationToken cancellationToken = default);
}