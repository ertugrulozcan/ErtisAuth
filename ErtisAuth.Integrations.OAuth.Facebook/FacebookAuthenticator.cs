using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Core;
using ErtisAuth.Integrations.OAuth.Facebook.Models;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Integrations.OAuth.Facebook
{
	public interface IFacebookAuthenticator : 
		IProviderAuthenticator,
		IProviderAuthenticator<FacebookLoginRequest, FacebookUserToken, FacebookUserToken>
	{
		Task<bool> VerifyLimitedTokenAsync(FacebookLoginRequest request, CancellationToken cancellationToken = default);
	}
	
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
			var facebookLoginRequest = request as FacebookLoginRequest;
			if (facebookLoginRequest == null)
			{
				throw ErtisAuthException.Unauthorized($"Token was not verified by provider (FacebookLoginRequest payload is null)");
				// return false;
			}
			
			if (facebookLoginRequest.IsLimited)
			{
				return await this.VerifyLimitedTokenAsync(facebookLoginRequest, cancellationToken);
			}
			else
			{
				return await this.VerifyTokenAsync(facebookLoginRequest, provider, cancellationToken);
			}
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

		public async Task<bool> VerifyLimitedTokenAsync(FacebookLoginRequest request, CancellationToken cancellationToken = default)
		{
			var response = await this.restHandler.ExecuteRequestAsync<JWKeys>(
				HttpMethod.Get,
				"https://www.facebook.com/.well-known/oauth/openid/jwks/",
				QueryString.Empty,
				HeaderCollection.Empty, 
				cancellationToken: cancellationToken);
			
			if (response.IsSuccess && response.Data?.Keys != null && response.Data.Keys.Any())
			{
				var jwk = JsonWebKeySet.Create(response.Json);
				
				var tokenHandler = new JwtSecurityTokenHandler();
				try
				{
					var result = tokenHandler.ValidateToken(request.AccessToken, new TokenValidationParameters
					{
						ValidateIssuerSigningKey = true,
						ValidateIssuer = true,
						ValidateAudience = true,
						ValidIssuer = "https://www.facebook.com",
						ValidAudience = request.AppId,
						IssuerSigningKey = jwk.Keys.First(),
						RequireExpirationTime = true,
						RequireSignedTokens = true,
					}, out var validatedToken);
					
					Console.WriteLine($"Validated limited token id: {validatedToken.Id}");
					var isAuthenticated = result?.Identity is { IsAuthenticated: true };

					if (!isAuthenticated)
					{
						throw ErtisAuthException.Unauthorized("Token was not verified by provider (Identity is not authenticated)");
					}

					return true;
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
					// return false;
					
					throw ErtisAuthException.Unauthorized($"Token was not verified by provider ({ex.Message})");
				}
			}
			else
			{
				// return false;
				throw ErtisAuthException.Unauthorized($"Token was not verified by provider (Json web key set could not retrieved by facebook)");
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