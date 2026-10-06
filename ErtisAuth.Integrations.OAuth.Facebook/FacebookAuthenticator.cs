using Ertis.Core.Models;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Facebook.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// ReSharper disable UnusedMemberInSuper.Global
namespace ErtisAuth.Integrations.OAuth.Facebook;

public interface IFacebookAuthenticator : IProviderAuthenticator<FacebookProvider, FacebookLoginRequest, FacebookUserToken, FacebookUserToken>;

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
		IQueryString? queryString = null,
		IHeaderCollection? headers = null,
		IRequestBody? body = null,
		CancellationToken cancellationToken = default)
	{
		return await this.restHandler.ExecuteRequestAsync(method, baseUrl, queryString, headers, body, cancellationToken);
	}
	
	private async Task<IResponseResult<TResult>> ExecuteRequestAsync<TResult>(
		HttpMethod method,
		string baseUrl,
		IQueryString? queryString = null,
		IHeaderCollection? headers = null,
		IRequestBody? body = null,
		CancellationToken cancellationToken = default)
	{
		return await this.restHandler.ExecuteRequestAsync<TResult>(method, baseUrl, queryString, headers, body, cancellationToken: cancellationToken);
	}
	
	public async Task<bool> VerifyTokenAsync(FacebookLoginRequest request, FacebookProvider provider, CancellationToken cancellationToken = default)
	{
		if (request.IsLimited)
		{
			return await this.VerifyLimitedTokenAsync(request, provider, cancellationToken);
		}
		else
		{
			return await this.VerifyStandardTokenAsync(request, provider, cancellationToken);
		}
	}
	
	private async Task<bool> VerifyStandardTokenAsync(FacebookLoginRequest request, FacebookProvider provider, CancellationToken cancellationToken = default)
	{
		if (!request.IsValid())
		{
			throw ErtisAuthException.InvalidToken("Invalid provider payload");
		}
		
		if (string.IsNullOrEmpty(request.User?.AccessToken))
		{
			return false;
		}
		
		if (provider.AppClientId != request.AppId)
		{
			throw ErtisAuthException.UntrustedProvider();
		}
		
		var response = await this.ExecuteRequestAsync<VerifyTokenResponse>(
			HttpMethod.Get, 
			$"{FACEBOOK_GRAPH_API_URL}/debug_token",
			QueryString
				.Add("input_token", request.User.AccessToken)
				.Add("access_token", request.User.AccessToken), 
			cancellationToken: cancellationToken);
		
		var isVerified =
			response is { IsSuccess: true, Data.Data.IsValid: true } &&
			response.Data.Data.AppId == provider.AppClientId &&
			response.Data.Data.UserId == request.User.Id;
		
		if (!isVerified)
		{
			return false;
		}
		
		// The profile in the payload is client-controlled; read it from Facebook with the verified access token
		var profileResponse = await this.ExecuteRequestAsync<FacebookUserToken>(
			HttpMethod.Get, 
			$"{FACEBOOK_GRAPH_API_URL}/me",
			QueryString
				.Add("fields", "id,first_name,last_name,email,picture")
				.Add("access_token", request.User.AccessToken), 
			cancellationToken: cancellationToken);
		
		if (profileResponse is not { IsSuccess: true, Data: not null } || profileResponse.Data.Id != request.User.Id)
		{
			return false;
		}
		
		var profile = profileResponse.Data;
		request.User.EmailAddress = string.IsNullOrEmpty(profile.EmailAddress) ? null : profile.EmailAddress;
		request.User.FirstName = string.IsNullOrEmpty(profile.FirstName) ? request.User.FirstName : profile.FirstName;
		request.User.LastName = string.IsNullOrEmpty(profile.LastName) ? request.User.LastName : profile.LastName;
		request.User.Picture = profile.Picture;
		return true;
	}
	
	private async Task<bool> VerifyLimitedTokenAsync(FacebookLoginRequest request, FacebookProvider provider, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(provider.AppClientId) || provider.AppClientId != request.AppId)
		{
			throw ErtisAuthException.UntrustedProvider();
		}
		
		var response = await this.restHandler.ExecuteRequestAsync<JWKeys>(
			HttpMethod.Get,
			"https://www.facebook.com/.well-known/oauth/openid/jwks/",
			QueryString.Empty,
			HeaderCollection.Empty, 
			cancellationToken: cancellationToken);
		
		if (response is { IsSuccess: true, Data.Keys: not null } && response.Data.Keys.Any())
		{
			var tokenHandler = new JsonWebTokenHandler();
			var jwk = JsonWebKeySet.Create(response.Json);
			var validationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				ValidateIssuer = true,
				ValidateAudience = true,
				ValidIssuer = "https://www.facebook.com",
				// The audience is the app configured on the provider, not the app id sent by the client
				ValidAudience = provider.AppClientId,
				// Facebook publishes several keys (rotation)
				IssuerSigningKeys = jwk.Keys,
				RequireExpirationTime = true,
				RequireSignedTokens = true
			};
			
			var validation = await tokenHandler.ValidateTokenAsync(request.AccessToken, validationParameters);
			if (!validation.IsValid || validation.SecurityToken is not JsonWebToken jwt || string.IsNullOrEmpty(jwt.Subject))
			{
				throw ErtisAuthException.Unauthorized("Token was not verified by provider (Identity is not authenticated)");
			}
			
			ApplyIdentity(request, jwt);
			return true;
		}
		else
		{
			throw ErtisAuthException.Unauthorized("Token was not verified by provider (Json web key set could not retrieved by facebook)");
		}
	}
	
	/// <summary>
	/// The identity comes only from the verified Limited Login token; the client payload is not trusted.
	/// </summary>
	private static void ApplyIdentity(FacebookLoginRequest request, JsonWebToken jwt)
	{
		request.User ??= new FacebookUserToken();
		request.User.Id = jwt.Subject;
		request.User.EmailAddress = GetClaim(jwt, "email");
		request.User.FirstName = GetClaim(jwt, "given_name") ?? request.User.FirstName;
		request.User.LastName = GetClaim(jwt, "family_name") ?? request.User.LastName;
		
		var picture = GetClaim(jwt, "picture");
		request.User.Picture = picture == null ? null : new FacebookImageData { Data = new FacebookImage { Url = picture } };
	}
	
	private static string? GetClaim(JsonWebToken jwt, string claimType)
	{
		return jwt.TryGetPayloadValue<string>(claimType, out var value) && !string.IsNullOrEmpty(value) ? value : null;
	}
	
	public async Task<bool> RevokeTokenAsync(string accessToken, FacebookProvider provider, CancellationToken cancellationToken = default)
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