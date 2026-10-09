using System.Net;
using System.Text.Json;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Services.Interfaces;
using ErtisAuth.Sdk.Tests.Helpers;

namespace ErtisAuth.Sdk.Tests.Services;

/// <summary>
/// The requests AuthenticationService sends to the ErtisAuth API (tokens controller) and how it reads the responses.
/// </summary>
public class AuthenticationServiceTests
{
	#region Fields
	
	private readonly SdkTestServices _sdk = new();
	
	private readonly IAuthenticationService _authenticationService;
	
	#endregion
	
	#region Constructors
	
	public AuthenticationServiceTests()
	{
		this._authenticationService = this._sdk.Get<IAuthenticationService>();
	}
	
	#endregion
	
	#region Helpers
	
	private RecordedRequest LastRequest => this._sdk.Handler.LastRequest;
	
	/// <summary>
	/// Request-shape tests of the token endpoints answer with an error, so that they only check the request and not
	/// the parsing of successful token responses.
	/// </summary>
	private void RespondWithError()
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.BadRequest;
	}
	
	/// <summary>
	/// A bearer token as the API serializes it in the generate-token and refresh-token responses.
	/// </summary>
	private static string ServerBearerTokenJson()
	{
		return JsonSerializer.Serialize(new BearerToken("access-token", TimeSpan.FromHours(1), "refresh-token", TimeSpan.FromHours(2)));
	}
	
	#endregion
	
	#region Generate Token
	
	[Fact]
	public async Task GetTokenAsync_PostsCredentialsWithMembershipHeader()
	{
		this.RespondWithError();
		
		await this._authenticationService.GetTokenAsync("john.doe", "P@ssw0rd", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Post, this.LastRequest.Method);
		Assert.Equal($"{SdkTestServices.BaseUrl}/generate-token", this.LastRequest.Path);
		Assert.Equal(SdkTestServices.MembershipId, this.LastRequest.Header("Membership"));
		Assert.Equal("""{"username":"john.doe","password":"P@ssw0rd"}""", this.LastRequest.Body);
		Assert.Null(this.LastRequest.Header("X-IpAddress"));
		Assert.Null(this.LastRequest.Header("X-UserAgent"));
	}
	
	[Fact]
	public async Task GetTokenAsync_WithClientInfo_SendsIpAddressAndUserAgentHeaders()
	{
		this.RespondWithError();
		
		await this._authenticationService.GetTokenAsync("john.doe", "P@ssw0rd", "10.0.0.1", "test-agent", TestContext.Current.CancellationToken);
		
		Assert.Equal("10.0.0.1", this.LastRequest.Header("X-IpAddress"));
		Assert.Equal("test-agent", this.LastRequest.Header("X-UserAgent"));
	}
	
	[Fact]
	public async Task GetTokenAsync_WithSuccessfulResponse_ReturnsBearerToken()
	{
		var createdAt = new DateTime(2026, 9, 28, 10, 30, 0, DateTimeKind.Utc);
		this._sdk.Handler.ResponseJson = JsonSerializer.Serialize(new BearerToken("access-token", TimeSpan.FromHours(1), "refresh-token", TimeSpan.FromHours(2), createdAt));
		
		var response = await this._authenticationService.GetTokenAsync("john.doe", "P@ssw0rd", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.True(response.IsSuccess);
		Assert.NotNull(response.Data);
		Assert.Equal("access-token", response.Data.AccessToken);
		Assert.Equal("refresh-token", response.Data.RefreshToken);
		Assert.Equal(3600, response.Data.ExpiresInTimeStamp);
		Assert.Equal(TimeSpan.FromHours(2), response.Data.RefreshExpiresIn);
		Assert.Equal(createdAt, response.Data.CreatedAt);
		Assert.Equal(DateTimeKind.Utc, response.Data.CreatedAt.Kind);
	}
	
	[Fact]
	public async Task RefreshTokenAsync_WithSuccessfulResponse_ReturnsBearerToken()
	{
		this._sdk.Handler.ResponseJson = ServerBearerTokenJson();
		
		var response = await this._authenticationService.RefreshTokenAsync("refresh-token", TestContext.Current.CancellationToken);
		
		Assert.True(response.IsSuccess);
		Assert.Equal("access-token", response.Data?.AccessToken);
		Assert.Equal("refresh-token", response.Data?.RefreshToken);
	}
	
	[Fact]
	public async Task GetTokenAsync_WithErrorResponse_ReturnsFailureWithStatusCode()
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.Unauthorized;
		this._sdk.Handler.ResponseJson = """{"message":"Username or password is wrong","errorCode":"InvalidCredentials","statusCode":401}""";
		
		var response = await this._authenticationService.GetTokenAsync("john.doe", "wrong", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.False(response.IsSuccess);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Null(response.Data);
	}
	
	#endregion
	
	#region Refresh Token
	
	[Fact]
	public async Task RefreshTokenAsync_WithBearerToken_SendsRefreshTokenInAuthorizationHeader()
	{
		this.RespondWithError();
		
		var bearerToken = new BearerToken("access-token", TimeSpan.FromHours(1), "refresh-token", TimeSpan.FromHours(2));
		
		await this._authenticationService.RefreshTokenAsync(bearerToken, TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Get, this.LastRequest.Method);
		Assert.Equal($"{SdkTestServices.BaseUrl}/refresh-token", this.LastRequest.Path);
		// Without a token type prefix; the API accepts both forms
		Assert.Equal("refresh-token", this.LastRequest.Header("Authorization"));
	}
	
	[Fact]
	public async Task RefreshTokenAsync_WithRefreshTokenString_SendsBearerAuthorizationHeader()
	{
		this.RespondWithError();
		
		await this._authenticationService.RefreshTokenAsync("refresh-token", TestContext.Current.CancellationToken);
		
		Assert.Equal("Bearer refresh-token", this.LastRequest.Header("Authorization"));
	}
	
	[Fact]
	public async Task RefreshTokenAsync_WithoutRefreshToken_ThrowsWithoutSendingRequest()
	{
		var bearerToken = BearerToken.CreateTemp("access-token");
		
		var exception = await Assert.ThrowsAsync<ErtisAuthException>(() => this._authenticationService.RefreshTokenAsync(bearerToken, TestContext.Current.CancellationToken));
		
		Assert.Equal("TokenIsNotRefreshable", exception.ErrorCode);
		Assert.Empty(this._sdk.Handler.Requests);
	}
	
	#endregion
	
	#region Verify & Revoke Token
	
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task VerifyTokenAsync_SendsBearerAuthorizationHeader(bool asString)
	{
		if (asString)
		{
			await this._authenticationService.VerifyTokenAsync("access-token", TestContext.Current.CancellationToken);
		}
		else
		{
			await this._authenticationService.VerifyTokenAsync(BearerToken.CreateTemp("access-token"), TestContext.Current.CancellationToken);
		}
		
		Assert.Equal(HttpMethod.Get, this.LastRequest.Method);
		Assert.Equal($"{SdkTestServices.BaseUrl}/verify-token", this.LastRequest.Path);
		Assert.Equal("Bearer access-token", this.LastRequest.Header("Authorization"));
	}
	
	[Theory]
	[InlineData(false, "access_token")]
	[InlineData(true, "refresh_token")]
	public async Task VerifyTokenAsync_WithSuccessfulResponse_ReturnsValidationResult(bool isRefreshToken, string expectedTokenKind)
	{
		this._sdk.Handler.ResponseJson = JsonSerializer.Serialize(new BearerTokenValidationResult(true, "access-token", null, TimeSpan.FromMinutes(30), isRefreshToken));
		
		var response = await this._authenticationService.VerifyTokenAsync("access-token", TestContext.Current.CancellationToken);
		
		Assert.True(response.IsSuccess);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var validationResult = Assert.IsType<BearerTokenValidationResult>(response.Data);
		Assert.True(validationResult.IsValidated);
		Assert.Equal("access-token", validationResult.Token);
		Assert.Equal(TimeSpan.FromMinutes(30), validationResult.RemainingTime);
		Assert.Equal(expectedTokenKind, validationResult.TokenKind);
		Assert.Equal(isRefreshToken, validationResult.IsRefreshToken);
	}
	
	[Fact]
	public async Task RevokeTokenAsync_SendsBearerAuthorizationHeaderWithoutQuery()
	{
		await this._authenticationService.RevokeTokenAsync("access-token", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.Equal(HttpMethod.Get, this.LastRequest.Method);
		Assert.Equal($"{SdkTestServices.BaseUrl}/revoke-token", this.LastRequest.Path);
		Assert.Equal("Bearer access-token", this.LastRequest.Header("Authorization"));
		Assert.Equal(string.Empty, this.LastRequest.Query);
	}
	
	[Fact]
	public async Task RevokeTokenAsync_WithNoContentResponse_Succeeds()
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.NoContent;
		this._sdk.Handler.ResponseJson = null;
		
		var response = await this._authenticationService.RevokeTokenAsync("access-token", cancellationToken: TestContext.Current.CancellationToken);
		
		Assert.True(response.IsSuccess);
	}
	
	[Fact]
	public async Task VerifyTokenAsync_WithErrorResponse_KeepsStatusCodeWithoutData()
	{
		this._sdk.Handler.ResponseStatusCode = HttpStatusCode.Unauthorized;
		this._sdk.Handler.ResponseJson = """{"message":"Token was expired","errorCode":"TokenWasExpired","statusCode":401}""";
		
		var response = await this._authenticationService.VerifyTokenAsync("access-token", TestContext.Current.CancellationToken);
		
		Assert.False(response.IsSuccess);
		Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
		Assert.Null(response.Data);
	}
	
	[Fact]
	public async Task RevokeTokenAsync_FromAllDevices_SendsLogoutAllQuery()
	{
		await this._authenticationService.RevokeTokenAsync("access-token", logoutFromAllDevices: true, cancellationToken: TestContext.Current.CancellationToken);
		
		// The API parses the value with bool.TryParse
		Assert.True(bool.Parse(System.Web.HttpUtility.ParseQueryString(this.LastRequest.Query)["logout-all"]!));
	}
	
	#endregion
	
	#region Me & WhoAmI
	
	[Theory]
	[InlineData("me")]
	[InlineData("whoami")]
	public async Task MeAndWhoAmI_WithBearerToken_ReturnUser(string endpoint)
	{
		this._sdk.Handler.ResponseJson = """{"_id":"5f8a1b2c3d4e5f6a7b8c9d01","username":"john.doe","role":"admin","membership_id":"5f8a1b2c3d4e5f6a7b8c9d00"}""";
		var bearerToken = BearerToken.CreateTemp("access-token");
		
		var response = endpoint == "me"
			? await this._authenticationService.MeAsync(bearerToken, TestContext.Current.CancellationToken)
			: await this._authenticationService.WhoAmIAsync(bearerToken, TestContext.Current.CancellationToken);
		
		Assert.Equal($"{SdkTestServices.BaseUrl}/{endpoint}", this.LastRequest.Path);
		Assert.Equal("Bearer access-token", this.LastRequest.Header("Authorization"));
		Assert.True(response.IsSuccess);
		Assert.Equal("john.doe", response.Data?.Username);
	}
	
	[Fact]
	public async Task WhoAmIAsync_WithBasicToken_ReturnsApplication()
	{
		this._sdk.Handler.ResponseJson = """{"_id":"5f8a1b2c3d4e5f6a7b8c9d02","name":"server-app","role":"server","membership_id":"5f8a1b2c3d4e5f6a7b8c9d00"}""";
		
		var response = await this._authenticationService.WhoAmIAsync(new BasicToken("5f8a1b2c3d4e5f6a7b8c9d02:secret"), TestContext.Current.CancellationToken);
		
		Assert.Equal($"{SdkTestServices.BaseUrl}/whoami", this.LastRequest.Path);
		Assert.Equal("Basic 5f8a1b2c3d4e5f6a7b8c9d02:secret", this.LastRequest.Header("Authorization"));
		Assert.Equal("server-app", response.Data?.Name);
	}
	
	#endregion
}
