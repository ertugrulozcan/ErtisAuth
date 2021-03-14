using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IPasswordService
	{
		IResponseResult<ResetPasswordToken> ResetPassword(string emailAddress);

		Task<IResponseResult<ResetPasswordToken>> ResetPasswordAsync(string emailAddress);

		IResponseResult SetPassword(string email, string password, string resetToken);

		Task<IResponseResult> SetPasswordAsync(string email, string password, string resetToken);
		
		IResponseResult ChangePassword(string userId, string newPassword, string accessToken);

		Task<IResponseResult> ChangePasswordAsync(string userId, string newPassword, string accessToken);
	}
}