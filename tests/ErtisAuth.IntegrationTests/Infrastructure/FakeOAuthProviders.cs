using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ErtisAuth.Integrations.OAuth.Google;
using Google.Apis.Auth;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

// ReSharper disable NotAccessedPositionalProperty.Global
namespace ErtisAuth.IntegrationTests.Infrastructure;

/// <summary>
/// The external OAuth providers (Facebook, Microsoft, Apple) as ErtisAuth calls them over HTTP: requests to their hosts
/// are answered from the routes the tests register (by url without query) and recorded; any other request (e.g. a
/// webhook to a local receiver) goes to the network. An unregistered provider url gets 404, so it fails visibly.
/// </summary>
public sealed class FakeOAuthProviders
{
	#region Constants
	
	public const string FacebookDebugTokenUrl = "https://graph.facebook.com/debug_token";
	
	public const string FacebookMeUrl = "https://graph.facebook.com/me";
	
	public const string MicrosoftMeUrl = "https://graph.microsoft.com/v1.0/me";
	
	public const string AppleTokenUrl = "https://appleid.apple.com/auth/token";
	
	public const string AppleRevokeUrl = "https://appleid.apple.com/auth/revoke";
	
	public const string AppleIssuer = "https://appleid.apple.com";
	
	private static readonly string[] ProviderHosts =
	[
		"graph.facebook.com",
		"www.facebook.com",
		"graph.microsoft.com",
		"appleid.apple.com"
	];
	
	#endregion
	
	#region Fields
	
	private readonly ConcurrentDictionary<string, (HttpStatusCode StatusCode, string Body)> _routes = new(StringComparer.OrdinalIgnoreCase);
	
	private readonly ConcurrentQueue<ProviderRequest> _requests = new();
	
	private readonly RsaSecurityKey _appleKey = new(RSA.Create(2048)) { KeyId = "apple-test-key" };
	
	#endregion
	
	#region Properties
	
	// ReSharper disable once MemberCanBePrivate.Global
	public IReadOnlyCollection<ProviderRequest> Requests => this._requests.ToArray();
	
	/// <summary>
	/// Google ID tokens are validated by GoogleJsonWebSignature with its own HTTP client: replaced by this validator.
	/// </summary>
	public FakeGoogleIdTokenValidator Google { get; } = new();
	
	#endregion
	
	#region Methods
	
	public void Respond(string url, object body, HttpStatusCode statusCode = HttpStatusCode.OK)
	{
		this._routes[url] = (statusCode, body as string ?? JsonSerializer.Serialize(body));
	}
	
	public IEnumerable<ProviderRequest> RequestsTo(string url) => this.Requests.Where(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase));
	
	/// <summary>
	/// A primary handler for the app's HTTP clients (a new one each time: the client factory disposes its handlers).
	/// </summary>
	public HttpMessageHandler CreateHandler() => new Handler(this);
	
	/// <summary>
	/// Apple's token endpoint answering a code exchange with an id_token of these claims (signed by a test key:
	/// ErtisAuth trusts the id_token of the TLS-protected token endpoint response).
	/// </summary>
	public void RespondToAppleCodeExchange(string audience, string sub, string email, bool emailVerified = true)
	{
		var idToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
		{
			Issuer = AppleIssuer,
			Audience = audience,
			Claims = new Dictionary<string, object> { ["sub"] = sub, ["email"] = email, ["email_verified"] = emailVerified ? "true" : "false" },
			Expires = DateTime.UtcNow.AddMinutes(10),
			SigningCredentials = new SigningCredentials(this._appleKey, SecurityAlgorithms.RsaSha256)
		});
		
		this.Respond(AppleTokenUrl, new
		{
			access_token = "apple-access-token",
			token_type = "Bearer",
			expires_in = 3600,
			refresh_token = "apple-refresh-token",
			id_token = idToken
		});
	}
	
	/// <summary>
	/// An EC P-256 private key in PEM format, like the Sign in with Apple key of a provider (signs the client secret).
	/// </summary>
	public static string CreateApplePrivateKeyPem()
	{
		using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
		return ecdsa.ExportPkcs8PrivateKeyPem();
	}
	
	/// <summary>
	/// An unsigned JWT with the given claims, as a client can craft it (e.g. the id_token in Apple's login payload).
	/// </summary>
	public static string CreateUnsignedToken(IDictionary<string, object> claims)
	{
		return new JsonWebTokenHandler().CreateToken(JsonSerializer.Serialize(claims));
	}
	
	#endregion
	
	#region Nested Types
	
	public sealed record ProviderRequest(HttpMethod Method, string Url, string Query, string? Authorization, string? Body);
	
	private sealed class Handler : HttpMessageHandler
	{
		private readonly FakeOAuthProviders _providers;
		
		private readonly HttpMessageInvoker _network = new(new SocketsHttpHandler());
		
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="providers"></param>
		public Handler(FakeOAuthProviders providers)
		{
			this._providers = providers;
		}
		
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var uri = request.RequestUri!;
			if (!ProviderHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
			{
				return await this._network.SendAsync(request, cancellationToken);
			}
			
			var url = uri.GetLeftPart(UriPartial.Path);
			var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
			this._providers._requests.Enqueue(new ProviderRequest(request.Method, url, uri.Query, request.Headers.Authorization?.ToString(), body));
			
			if (!this._providers._routes.TryGetValue(url, out var route))
			{
				return new HttpResponseMessage(HttpStatusCode.NotFound);
			}
			
			return new HttpResponseMessage(route.StatusCode)
			{
				Content = new StringContent(route.Body, Encoding.UTF8, "application/json")
			};
		}
		
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this._network.Dispose();
			}
			
			base.Dispose(disposing);
		}
	}
	
	#endregion
}

/// <summary>
/// Google ID tokens registered by the tests with their payload; any other token (or audience) is invalid.
/// </summary>
public sealed class FakeGoogleIdTokenValidator : IGoogleIdTokenValidator
{
	#region Fields
	
	private readonly ConcurrentDictionary<string, (string Audience, GoogleJsonWebSignature.Payload Payload)> _tokens = new();
	
	#endregion
	
	#region Methods
	
	public string Issue(string audience, string sub, string email, bool emailVerified, string givenName = "Jane", string familyName = "Doe")
	{
		var idToken = $"google-id-token-{Guid.NewGuid():N}";
		this._tokens[idToken] = (audience, new GoogleJsonWebSignature.Payload
		{
			Subject = sub,
			Email = email,
			EmailVerified = emailVerified,
			GivenName = givenName,
			FamilyName = familyName,
			Audience = audience,
			ExpirationTimeSeconds = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
		});
		
		return idToken;
	}
	
	public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string audience)
	{
		if (this._tokens.TryGetValue(idToken, out var token) && token.Audience == audience)
		{
			return Task.FromResult(token.Payload);
		}
		
		throw new InvalidJwtException("JWT invalid, unable to verify signature.");
	}
	
	#endregion
}