using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Integrations.OAuth.Facebook
{
	public interface IFacebookAuthenticator : IProviderAuthenticator, IProviderAuthenticator<FacebookLoginRequest, FacebookUserToken, FacebookUserToken>
	{}
	
	public class FacebookAuthenticator : IFacebookAuthenticator
	{
		#region Constants

		private const string FACEBOOK_GRAPH_API_URL = "https://graph.facebook.com";

		#endregion
		
		#region Services

		private readonly IRestHandler restHandler;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="restHandler"></param>
		public FacebookAuthenticator(IRestHandler restHandler)
		{
			this.restHandler = restHandler;
		}

		#endregion

		#region Methods

		private async Task<IResponseResult> ExecuteRequestAsync(
			HttpMethod method,
			string baseUrl,
			IQueryString queryString = null,
			IHeaderCollection headers = null,
			IRequestBody body = null,
			CancellationToken cancellationToken = default)
		{
			return await this.restHandler.ExecuteRequestAsync(method, baseUrl, queryString, headers, body, cancellationToken);
		}
		
		private async Task<IResponseResult<TResult>> ExecuteRequestAsync<TResult>(
			HttpMethod method,
			string baseUrl,
			IQueryString queryString = null,
			IHeaderCollection headers = null,
			IRequestBody body = null,
			CancellationToken cancellationToken = default)
		{
			return await this.restHandler.ExecuteRequestAsync<TResult>(method, baseUrl, queryString, headers, body, cancellationToken: cancellationToken);
		}
		
		public async Task<bool> VerifyTokenAsync(IProviderLoginRequest request, Provider provider, CancellationToken cancellationToken = default)
		{
			return await this.VerifyTokenAsync(request as FacebookLoginRequest, provider, cancellationToken);
		}
		
		public async Task<bool> VerifyTokenAsync(FacebookLoginRequest request, Provider provider, CancellationToken cancellationToken = default)
		{
			if (!request.IsValid())
			{
				throw ErtisAuthException.InvalidToken("Invalid provider payload");
			}

			if (provider.AppClientId == request.AppId)
			{
				var response = await this.ExecuteRequestAsync<VerifyTokenResponse>(
					HttpMethod.Get, 
					$"{FACEBOOK_GRAPH_API_URL}/debug_token",
					QueryString
						.Add("input_token", request.User.AccessToken)
						.Add("access_token", request.User.AccessToken), 
					cancellationToken: cancellationToken);

				return
					response.IsSuccess &&
					response.Data.Data is { IsValid: true } && 
					response.Data.Data.AppId == provider.AppClientId && 
					response.Data.Data.UserId == request.User.Id;
			}
			else
			{
				throw ErtisAuthException.UntrustedProvider();
			}
		}

		public async Task<bool> RevokeTokenAsync(string accessToken, Provider provider, CancellationToken cancellationToken = default)
		{
			var response = await this.ExecuteRequestAsync(
				HttpMethod.Delete, 
				$"{FACEBOOK_GRAPH_API_URL}/{provider.AppClientId}/permissions",
				QueryString.Add("access_token", accessToken),
				cancellationToken: cancellationToken);

			return response.IsSuccess;
		}
        
		#endregion
	}
}