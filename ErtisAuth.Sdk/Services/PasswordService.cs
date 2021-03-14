using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class PasswordService : MembershipBoundedService, IPasswordService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public PasswordService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
		
		#region Methods

		public IResponseResult<ResetPasswordToken> ResetPassword(string emailAddress)
		{
			throw new System.NotImplementedException();
		}

		public async Task<IResponseResult<ResetPasswordToken>> ResetPasswordAsync(string emailAddress)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult SetPassword(string email, string password, string resetToken)
		{
			throw new System.NotImplementedException();
		}

		public async Task<IResponseResult> SetPasswordAsync(string email, string password, string resetToken)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult ChangePassword(string userId, string newPassword, string accessToken)
		{
			throw new System.NotImplementedException();
		}

		public async Task<IResponseResult> ChangePasswordAsync(string userId, string newPassword, string accessToken)
		{
			throw new System.NotImplementedException();
		}

		#endregion
	}
}