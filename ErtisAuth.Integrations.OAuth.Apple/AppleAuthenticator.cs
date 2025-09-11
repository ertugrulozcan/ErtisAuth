using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Integrations.OAuth.Abstractions;
using ErtisAuth.Integrations.OAuth.Core;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;

namespace ErtisAuth.Integrations.OAuth.Apple;

public interface IAppleAuthenticator : IProviderAuthenticator,
	IProviderAuthenticator<AppleLoginRequestBase, AppleToken?, AppleUser?>;

public class AppleAuthenticator : IAppleAuthenticator
{
	#region Constants

	private const string VerifyTokenEndpoint = "https://appleid.apple.com/auth/token";
	private const string RevokeTokenEndpoint = "https://appleid.apple.com/auth/revoke";
	private const string Authority = "https://appleid.apple.com";

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
			
			var secret = GenerateAppleClientSecret(provider);
			if (string.IsNullOrEmpty(secret))
			{
				return false;
			}
			
			using var httpClient = new HttpClient();
			var response = await httpClient.PostAsync(VerifyTokenEndpoint, new FormUrlEncodedContent(new[]
			{
				new KeyValuePair<string, string>("client_id", provider.AppClientId),
				new KeyValuePair<string, string>("client_secret", secret),
				new KeyValuePair<string, string>("code", request.Token.Code),
				new KeyValuePair<string, string>("grant_type", "authorization_code"),
				new KeyValuePair<string, string>("redirect_uri", provider.RedirectUri)
			}), cancellationToken);
			
			if (response.StatusCode == HttpStatusCode.OK)
			{
				var appleBearerToken = JsonConvert.DeserializeObject<AppleBearerToken>(await response.Content.ReadAsStringAsync(cancellationToken));
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
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			return false;
		}
	}

	public async Task<bool> RevokeTokenAsync(string accessToken, Provider provider, CancellationToken cancellationToken = default)
	{
		try
		{
			var secret = GenerateAppleClientSecret(provider);
			if (string.IsNullOrEmpty(secret))
			{
				return false;
			}

			using var httpClient = new HttpClient();
			var response = await httpClient.PostAsync(RevokeTokenEndpoint, new FormUrlEncodedContent(new[]
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
			Console.WriteLine(ex);
			return false;
		}
	}
	
	private static string GenerateAppleClientSecret(Provider provider)
	{
		var ecdsa = ECDsa.Create();
		ecdsa.ImportPkcs8PrivateKey(Convert.FromBase64String(ClearPrivateKey(provider.PrivateKey)), out _);

		var jwtHeader = new JwtHeader(new SigningCredentials(new ECDsaSecurityKey(ecdsa), SecurityAlgorithms.EcdsaSha256));
		jwtHeader.Clear();
		jwtHeader.Add("alg", "ES256");
		jwtHeader.Add("kid", provider.PrivateKeyId);

		var jwtPayload = new JwtPayload(
			provider.TeamId,
			Authority,
			new List<Claim>
			{
				new ("sub", provider.AppClientId)
			},
			DateTime.UtcNow,
			DateTime.UtcNow.Add(TimeSpan.FromMinutes(5))
		);

		var jwt = new JwtSecurityToken(jwtHeader, jwtPayload);
		return new JwtSecurityTokenHandler().WriteToken(jwt);
	}
	
	private static string ClearPrivateKey(string privateKey)
	{
		return privateKey.Replace("-----BEGIN PRIVATE KEY-----\n", string.Empty).Replace("\n-----END PRIVATE KEY-----", string.Empty);
	}
    
	#endregion
}