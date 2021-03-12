using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class AuthenticationService : MembershipBoundedService, IAuthenticationService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public AuthenticationService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
		
		#region Methods

		public IResponseResult<BearerToken> GetToken(string username, string password)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult<BearerToken>> GetTokenAsync(string username, string password)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult<BearerToken> RefreshToken(BearerToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult<BearerToken>> RefreshTokenAsync(BearerToken token)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult<BearerToken> RefreshToken(string refreshToken)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult<BearerToken>> RefreshTokenAsync(string refreshToken)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult<BearerToken> VerifyToken(BearerToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult<BearerToken>> VerifyTokenAsync(BearerToken token)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult<BearerToken> VerifyToken(string accessToken)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult<BearerToken>> VerifyTokenAsync(string accessToken)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult RevokeToken(BearerToken token)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult> RevokeTokenAsync(BearerToken token)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult RevokeToken(string accessToken)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult> RevokeTokenAsync(string accessToken)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult<User> WhoAmI(string token)
		{
			var url = $"{this.AuthApiBaseUrl}/whoami";
			var headers = HeaderCollection.Add("Authorization", $"Bearer {token}");
			return this.ExecuteRequest<User>(HttpMethod.Get, url, null, headers);
		}

		public async Task<IResponseResult<User>> WhoAmIAsync(string token)
		{
			var url = $"{this.AuthApiBaseUrl}/whoami";
			var headers = HeaderCollection.Add("Authorization", $"Bearer {token}");
			return await this.ExecuteRequestAsync<User>(HttpMethod.Get, url, null, headers);
		}

		public IResponseResult<ResetPasswordToken> ResetPassword(string emailAddress)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult<ResetPasswordToken>> ResetPasswordAsync(string emailAddress)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult SetPassword(string email, string password, string resetToken)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult> SetPasswordAsync(string email, string password, string resetToken)
		{
			throw new System.NotImplementedException();
		}

		public IResponseResult ChangePassword(string userId, string newPassword, string accessToken)
		{
			throw new System.NotImplementedException();
		}

		public Task<IResponseResult> ChangePasswordAsync(string userId, string newPassword, string accessToken)
		{
			throw new System.NotImplementedException();
		}
		
		#endregion
	}
}