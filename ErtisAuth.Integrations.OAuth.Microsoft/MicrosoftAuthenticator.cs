using System;
using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Core;

namespace ErtisAuth.Integrations.OAuth.Microsoft
{
	public interface IMicrosoftAuthenticator : IProviderAuthenticator, IProviderAuthenticator<MicrosoftLoginRequest, MicrosoftToken, MicrosoftUser>
	{}
	
	public class MicrosoftAuthenticator : IMicrosoftAuthenticator
	{
		#region Constants

		private const string MICROSOFT_GRAPH_API_URL = "https://graph.microsoft.com/v1.0";

		#endregion
		
		#region Services

		private readonly IRestHandler restHandler;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="restHandler"></param>
		public MicrosoftAuthenticator(IRestHandler restHandler)
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
			IRequestBody body = null)
		{
			return await this.restHandler.ExecuteRequestAsync(method, baseUrl, queryString, headers, body);
		}
		
		private async Task<IResponseResult<TResult>> ExecuteRequestAsync<TResult>(
			HttpMethod method,
			string baseUrl,
			IQueryString queryString = null,
			IHeaderCollection headers = null,
			IRequestBody body = null)
		{
			return await this.restHandler.ExecuteRequestAsync<TResult>(method, baseUrl, queryString, headers, body);
		}
		
		public async Task<bool> VerifyTokenAsync(IProviderLoginRequest request, Provider provider)
		{
			return await this.VerifyTokenAsync(request as MicrosoftLoginRequest, provider);
		}
		
		public async Task<bool> VerifyTokenAsync(MicrosoftLoginRequest request, Provider provider)
		{
			if (provider.AppClientId == request.ClientId)
			{
				var response = await this.ExecuteRequestAsync<MicrosoftUser>(
					HttpMethod.Get, 
					$"{MICROSOFT_GRAPH_API_URL}/me",
					headers: HeaderCollection.Add("Authorization", $"Bearer {request.AccessToken}"));

				if (response.IsSuccess)
				{
					request.User = response.Data;	
				}
				
				return response.IsSuccess;
			}
			else
			{
				throw ErtisAuthException.UntrustedProvider();
			}
		}

		public async Task<bool> RevokeTokenAsync(string accessToken, Provider provider)
		{
			/*
			 * https://learn.microsoft.com/en-us/azure/active-directory/develop/active-directory-configurable-token-lifetimes#access-tokens
			 * 
			 * Clients use access tokens to access a protected resource. An access token can be used only for a specific combination of user, client, and resource.
			 * Access tokens cannot be revoked and are valid until their expiry.
			 * A malicious actor that has obtained an access token can use it for extent of its lifetime.
			 * Adjusting the lifetime of an access token is a trade-off between improving system performance and increasing the amount of time that the client retains access after the user's account is disabled.
			 * Improved system performance is achieved by reducing the number of times a client needs to acquire a fresh access token.
			 * The default lifetime of an access token is variable. When issued, an access token's default lifetime is assigned a random value ranging between 60-90 minutes (75 minutes on average).
			 * The default lifetime also varies depending on the client application requesting the token or if conditional access is enabled in the tenant. For more information, see Access token lifetime.
			*/
			
			await Task.CompletedTask;
			return true;
		}
        
		#endregion
	}
}