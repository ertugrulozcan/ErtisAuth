using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ErtisAuth.Integrations.OAuth.Tests.Helpers;

/// <summary>
/// Signing keys and JWTs standing in for the providers' (Apple, Facebook) keys and tokens.
/// </summary>
internal sealed class TestJwt
{
	#region Fields
	
	private readonly JsonWebTokenHandler _tokenHandler = new();
	
	#endregion
	
	#region Properties
	
	public RsaSecurityKey Key { get; }
	
	#endregion
	
	#region Constructors
	
	public TestJwt(string keyId)
	{
		this.Key = new RsaSecurityKey(RSA.Create(2048)) { KeyId = keyId };
	}
	
	#endregion
	
	#region Methods
	
	public string CreateToken(string issuer, string audience, IDictionary<string, object> claims, DateTime? expires = null)
	{
		var now = DateTime.UtcNow;
		return this._tokenHandler.CreateToken(new SecurityTokenDescriptor
		{
			Issuer = issuer,
			Audience = audience,
			Claims = claims,
			IssuedAt = expires is { } e && e < now ? e.AddMinutes(-10) : now,
			NotBefore = expires is { } n && n < now ? n.AddMinutes(-10) : now,
			Expires = expires ?? now.AddMinutes(10),
			SigningCredentials = new SigningCredentials(this.Key, SecurityAlgorithms.RsaSha256)
		});
	}
	
	/// <summary>
	/// A JWKS document ({"keys": [...]}) with the public part of the given keys.
	/// </summary>
	public static string JwksJson(params TestJwt[] keys)
	{
		var jwks = keys.Select(x =>
		{
			var parameters = x.Key.Rsa.ExportParameters(false);
			return new Dictionary<string, string>
			{
				["kty"] = "RSA",
				["use"] = "sig",
				["alg"] = "RS256",
				["kid"] = x.Key.KeyId,
				["n"] = Base64UrlEncoder.Encode(parameters.Modulus),
				["e"] = Base64UrlEncoder.Encode(parameters.Exponent)
			};
		});
		
		return JsonSerializer.Serialize(new { keys = jwks });
	}
	
	/// <summary>
	/// An unsigned token with arbitrary claims, as an attacker can craft it.
	/// </summary>
	public static string CreateUnsignedToken(IDictionary<string, object> claims)
	{
		return new JsonWebTokenHandler().CreateToken(JsonSerializer.Serialize(claims));
	}
	
	/// <summary>
	/// An EC P-256 private key in PEM format, like the Apple Sign in key a provider is configured with.
	/// </summary>
	public static string CreateApplePrivateKeyPem()
	{
		using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
		return ecdsa.ExportPkcs8PrivateKeyPem();
	}
	
	#endregion
}