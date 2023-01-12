using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
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
		
		public IResponseResult ChangePassword(string userId, string newPassword, TokenBase token) => this.ChangePasswordAsync(userId, newPassword, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> ChangePasswordAsync(string userId, string newPassword, TokenBase token, CancellationToken cancellationToken = default)
		{
			return await this.ExecuteRequestAsync(
				HttpMethod.Put, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/users/{userId}/change-password", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(new { password = newPassword }),
				cancellationToken: cancellationToken);
		}

		public IResponseResult<ResetPasswordToken> ResetPassword(string emailAddress, string server, string host, TokenBase token) => this.ResetPasswordAsync(emailAddress, server, host, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<ResetPasswordToken>> ResetPasswordAsync(string emailAddress, string server, string host, TokenBase token, CancellationToken cancellationToken = default)
		{
			return await this.ExecuteRequestAsync<ResetPasswordToken>(
				HttpMethod.Post, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/users/reset-password", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(new
				{
					email_address = emailAddress,
					server,
					host
				}),
				cancellationToken: cancellationToken);
		}

		public IResponseResult SetPassword(string email, string password, string resetToken, TokenBase token) => this.SetPasswordAsync(email, password, resetToken, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> SetPasswordAsync(string email, string password, string resetToken, TokenBase token, CancellationToken cancellationToken = default)
		{
			return await this.ExecuteRequestAsync(
				HttpMethod.Post, 
				$"{this.BaseUrl}/memberships/{this.MembershipId}/users/set-password", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(new { email_address = email, reset_token = resetToken, password }),
				cancellationToken: cancellationToken);
		}

		#endregion
	}
}