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
				if (appleBearerToken != null && !string.IsNullOrEmpty(appleBearerToken.AccessToken))
				{
					var identity = this.ReadIdentity(appleBearerToken.IdToken, provider);
					if (identity == null)
					{
						return false;
					}
					
					// Set AccessToken
					request.Token.AccessToken = appleBearerToken.AccessToken;
					this.ApplyIdentity(request, identity);
					return true;
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
	
	/// <summary>
	/// Reads the id_token returned by Apple's token endpoint. It is received directly from Apple over TLS in the code
	/// exchange, so TLS server validation replaces the signature check (OpenID Connect Core 3.1.3.7);
	/// issuer, audience and expiration are still verified.
	/// </summary>
	private JsonWebToken? ReadIdentity(string? idToken, Provider provider)
	{
		if (string.IsNullOrEmpty(idToken) || !this._tokenHandler.CanReadToken(idToken))
		{
			return null;
		}
		
		var jwt = this._tokenHandler.ReadJsonWebToken(idToken);
		var isValid =
			jwt.Issuer == Authority &&
			jwt.Audiences.Contains(provider.AppClientId) &&
			jwt.ValidTo > DateTime.UtcNow &&
			!string.IsNullOrEmpty(jwt.Subject);
		
		if (!isValid)
		{
			this._logger.LogWarning("Apple id_token rejected (issuer: {Issuer}, audiences: {Audiences}, expires: {Expires})", jwt.Issuer, string.Join(",", jwt.Audiences), jwt.ValidTo);
			return null;
		}
		
		return jwt;
	}
	
	/// <summary>
	/// The identity comes only from Apple's id_token; the client payload can only contribute the name,
	/// which Apple shares once, with the client.
	/// </summary>
	private void ApplyIdentity(AppleLoginRequestBase request, JsonWebToken identity)
	{
		request.User ??= new AppleUser();
		if (!string.IsNullOrEmpty(request.User.Id) && request.User.Id != identity.Subject)
		{
			this._logger.LogWarning("Apple login: the user id sent by the client does not match the id_token subject; the id_token subject is used");
		}
		
		request.User.Id = identity.Subject;
		request.User.EmailAddress = identity.TryGetPayloadValue<string>("email", out var email) && !string.IsNullOrEmpty(email) ? email : null;
		request.User.EmailVerified = request.User.EmailAddress != null && IsTrue(identity, "email_verified");
	}
	
	/// <summary>
	/// Apple sends boolean claims either as JSON booleans or as the strings "true" / "false".
	/// </summary>
	private static bool IsTrue(JsonWebToken jwt, string claimType)
	{
		var claim = jwt.Claims.FirstOrDefault(x => x.Type == claimType);
		return claim != null && bool.TryParse(claim.Value, out var value) && value;
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