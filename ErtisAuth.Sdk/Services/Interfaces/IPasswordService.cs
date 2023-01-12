using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	[ServiceLifetime(ServiceLifetime.Singleton)]
	public interface IPasswordService
	{
		IResponseResult ChangePassword(string userId, string newPassword, TokenBase token);

		Task<IResponseResult> ChangePasswordAsync(string userId, string newPassword, TokenBase token, CancellationToken cancellationToken = default);
		
		IResponseResult<ResetPasswordToken> ResetPassword(string emailAddress, string server, string host, TokenBase token);

		Task<IResponseResult<ResetPasswordToken>> ResetPasswordAsync(string emailAddress, string server, string host, TokenBase token, CancellationToken cancellationToken = default);

		IResponseResult SetPassword(string email, string password, string resetToken, TokenBase token);

		Task<IResponseResult> SetPasswordAsync(string email, string password, string resetToken, TokenBase token, CancellationToken cancellationToken = default);
	}
}