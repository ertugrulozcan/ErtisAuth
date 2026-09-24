using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Core;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Integrations.OAuth.Apple;

public interface IAppleAuthenticator : IProviderAuthenticator, IProviderAuthenticator<AppleLoginRequestBase, AppleToken, AppleUser>;

public class AppleAuthenticator : IAppleAuthenticator
{
	#region Constants
	
	private const string VerifyTokenEndpoint = "https://appleid.apple.com/auth/token";
	private const string RevokeTokenEndpoint = "https://appleid.apple.com/auth/revoke";
	private const string Authority = "https://appleid.apple.com";
	
	#endregion
	
	#region Services
	
	private readonly HttpClient _httpClient;
	private readonly JsonWebTokenHandler _tokenHandler;
	private readonly ILogger<AppleAuthenticator> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="httpClientFactory"></param>
	/// <param name="logger"></param>
	public AppleAuthenticator(IHttpClientFactory httpClientFactory, ILogger<AppleAuthenticator> logger)
	{
		this._httpClient = httpClientFactory.CreateClient();
		this._tokenHandler = new JsonWebTokenHandler();
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	public async Task<bool> VerifyTokenAsync(IProviderLoginRequest request, Provider provider, CancellationToken cancellationToken = default)
	{
		return await this.VerifyTokenAsync(request as AppleLoginRequestBase, provider, cancellationToken: cancellationToken);
	}
	
	public async Task<bool> VerifyTokenAsync(AppleLoginRequestBase? request, Provider provider, CancellationToken cancellationToken = default)
	{
		try
		{
			if (request?.Token == null || string.IsNullOrEmpty(request.Token.Code))
			{
				return false;
			}
			
			var secret = this.GenerateAppleClientSecret(provider);
			if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(provider.AppClientId) || string.IsNullOrEmpty(provider.RedirectUri))
			{
				return false;
			}
			
			var response = await this._httpClient.PostAsync(VerifyTokenEndpoint, new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("client_id", provider.AppClientId),
				new KeyValuePair<string, string>("client_secret", secret),
				new KeyValuePair<string, string>("code", request.Token.Code),
				new KeyValuePair<string, string>("grant_type", "authorization_code"),
				new KeyValuePair<string, string>("redirect_uri", provider.RedirectUri)
			}), cancellationToken);
			
			if (response.StatusCode == HttpStatusCode.OK)
			{
				var appleBearerToken = JsonSerializer.Deserialize<AppleBearerToken>(await response.Content.ReadAsStringAsync(cancellationToken));
				if (appleBearerToken != null)
				{
					// Set AccessToken
					request.Token.AccessToken = appleBearerToken.AccessToken;
					return !string.IsNullOrEmpty(appleBearerToken.AccessToken) && !string.IsNullOrEmpty(appleBearerToken.IdToken);
				}
			}
			else
			{
				var message = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
				throw ErtisAuthException.Unauthorized($"Token was not verified by provider ({message})");
			}
			
			return false;
		}
		catch (ErtisAuthException)
		{
			throw;
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "AppleAuthenticator.VerifyTokenAsync occured an error");
			return false;
		}
	}
	
	public async Task<bool> RevokeTokenAsync(string accessToken, Provider provider, CancellationToken cancellationToken = default)
	{
		try
		{
			var secret = this.GenerateAppleClientSecret(provider);
			if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(provider.AppClientId))
			{
				return false;
			}
			
			var response = await this._httpClient.PostAsync(RevokeTokenEndpoint, new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("client_id", provider.AppClientId),
				new KeyValuePair<string, string>("client_secret", secret),
				new KeyValuePair<string, string>("token", accessToken),
				new KeyValuePair<string, string>("token_type_hint", "access_token")
			}), cancellationToken);
			
			return response.StatusCode == HttpStatusCode.OK;
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "AppleAuthenticator.RevokeTokenAsync occured an error");
			return false;
		}
	}
	
	private string? GenerateAppleClientSecret(Provider provider)
	{
		if (string.IsNullOrEmpty(provider.PrivateKey) || string.IsNullOrEmpty(provider.AppClientId))
		{
			return null;
		}
		
		using var ecdsa = ECDsa.Create();
		ecdsa.ImportFromPem(provider.PrivateKey);
		
		var securityKey = new ECDsaSecurityKey(ecdsa)
		{
			KeyId = provider.PrivateKeyId,
			CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
		};
		
		var now = DateTime.UtcNow;
		
		var descriptor = new SecurityTokenDescriptor
		{
			Issuer = provider.TeamId,
			Audience = Authority,
			Claims = new Dictionary<string, object>
			{
				[JwtRegisteredClaimNames.Sub] = provider.AppClientId
			},
			IssuedAt = now,
			NotBefore = now,
			Expires = now.AddMinutes(5),
			SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.EcdsaSha256)
		};
		
		return this._tokenHandler.CreateToken(descriptor);
	}
	
	#endregion
}