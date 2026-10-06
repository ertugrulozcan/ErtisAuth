using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Integrations.OAuth.Apple;

public interface IAppleAuthenticator : IProviderAuthenticator<AppleProvider, AppleLoginRequestBase, AppleToken, AppleUser>;

public class AppleAuthenticator : IAppleAuthenticator
{
	#region Constants
	
	private const string VerifyTokenEndpoint = "https://appleid.apple.com/auth/token";
	private const string RevokeTokenEndpoint = "https://appleid.apple.com/auth/revoke";
	private const string Authority = "https://appleid.apple.com";
	
	#endregion
	
	#region Services
	
	private readonly IHttpClientFactory _httpClientFactory;
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
		// A client per request (not one for the lifetime of this singleton): the factory rotates its handlers, so DNS changes are picked up
		this._httpClientFactory = httpClientFactory;
		this._tokenHandler = new JsonWebTokenHandler();
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// Exchanges the authorization code. Only a code (or token) Apple does not accept is a failed login (false or 401);
	/// a provider configuration Apple can't use is ProviderNotConfiguredCorrectly, and Apple not answering (network
	/// error, timeout, 5xx, unreadable response) is ProviderUnavailable (503).
	/// </summary>
	public async Task<bool> VerifyTokenAsync(AppleLoginRequestBase request, AppleProvider provider, CancellationToken cancellationToken = default)
	{
		if (request.Token == null || string.IsNullOrEmpty(request.Token.Code))
		{
			return false;
		}
		
		if (string.IsNullOrEmpty(provider.AppClientId) || string.IsNullOrEmpty(provider.RedirectUri) || string.IsNullOrEmpty(provider.PrivateKey))
		{
			throw ErtisAuthException.ProviderNotConfiguredCorrectly("Sign in with Apple requires the app client id, the redirect uri and the private key");
		}
		
		var secret = this.CreateClientSecret(provider);
		
		HttpResponseMessage response;
		try
		{
			response = await this._httpClientFactory.CreateClient().PostAsync(VerifyTokenEndpoint, new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("client_id", provider.AppClientId),
				new KeyValuePair<string, string>("client_secret", secret),
				new KeyValuePair<string, string>("code", request.Token.Code),
				new KeyValuePair<string, string>("grant_type", "authorization_code"),
				new KeyValuePair<string, string>("redirect_uri", provider.RedirectUri)
			}), cancellationToken);
		}
		catch (HttpRequestException ex)
		{
			this._logger.LogError(ex, "Apple token endpoint could not be reached");
			throw ErtisAuthException.ProviderUnavailable("Apple");
		}
		catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
		{
			this._logger.LogError(ex, "Apple token endpoint timed out");
			throw ErtisAuthException.ProviderUnavailable("Apple");
		}
		
		using (response)
		{
			var content = await response.Content.ReadAsStringAsync(cancellationToken);
			if (response.StatusCode == HttpStatusCode.OK)
			{
				AppleBearerToken? appleBearerToken;
				try
				{
					appleBearerToken = JsonSerializer.Deserialize<AppleBearerToken>(content);
				}
				catch (JsonException ex)
				{
					this._logger.LogError(ex, "Apple token endpoint returned an unreadable response");
					throw ErtisAuthException.ProviderUnavailable("Apple");
				}
				
				if (appleBearerToken == null || string.IsNullOrEmpty(appleBearerToken.AccessToken))
				{
					return false;
				}
				
				var identity = this.ReadIdentity(appleBearerToken.IdToken, provider);
				if (identity == null)
				{
					return false;
				}
				
				request.Token.AccessToken = appleBearerToken.AccessToken;
				this.ApplyIdentity(request, identity);
				return true;
			}
			
			if ((int) response.StatusCode >= 500)
			{
				this._logger.LogError("Apple token endpoint answered {StatusCode}: {Content}", (int) response.StatusCode, content);
				throw ErtisAuthException.ProviderUnavailable("Apple");
			}
			
			// https://developer.apple.com/documentation/sign_in_with_apple/errorresponse
			var error = ReadErrorCode(content);
			if (error is "invalid_client" or "unauthorized_client")
			{
				this._logger.LogError("Apple rejected the client credentials of the provider: {Content}", content);
				throw ErtisAuthException.ProviderNotConfiguredCorrectly($"Apple rejected the client credentials ({error})");
			}
			
			throw ErtisAuthException.Unauthorized($"Token was not verified by provider ({content})");
		}
	}
	
	/// <summary>
	/// The client secret signed with the provider's private key; a key that can't be read is a configuration error.
	/// </summary>
	private string CreateClientSecret(AppleProvider provider)
	{
		try
		{
			var secret = this.GenerateAppleClientSecret(provider);
			return string.IsNullOrEmpty(secret)
				? throw ErtisAuthException.ProviderNotConfiguredCorrectly("The client secret could not be created")
				: secret;
		}
		catch (Exception ex) when (ex is ArgumentException or CryptographicException)
		{
			this._logger.LogError(ex, "The private key of the Apple provider could not be read");
			throw ErtisAuthException.ProviderNotConfiguredCorrectly("The private key could not be read");
		}
	}
	
	private static string? ReadErrorCode(string content)
	{
		try
		{
			using var document = JsonDocument.Parse(content);
			return document.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String ? error.GetString() : null;
		}
		catch (JsonException)
		{
			return null;
		}
	}
	
	public async Task<bool> RevokeTokenAsync(string accessToken, AppleProvider provider, CancellationToken cancellationToken = default)
	{
		try
		{
			var secret = this.GenerateAppleClientSecret(provider);
			if (string.IsNullOrEmpty(secret) || string.IsNullOrEmpty(provider.AppClientId))
			{
				return false;
			}
			
			var response = await this._httpClientFactory.CreateClient().PostAsync(RevokeTokenEndpoint, new FormUrlEncodedContent(new[]
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
	private JsonWebToken? ReadIdentity(string? idToken, AppleProvider provider)
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
	
	private string? GenerateAppleClientSecret(AppleProvider provider)
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