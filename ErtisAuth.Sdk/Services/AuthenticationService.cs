using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

// ReSharper disable UnusedType.Global
namespace ErtisAuth.Sdk.Services;

public class AuthenticationService : MembershipBoundedService, IAuthenticationService
{
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="ertisAuthOptions"></param>
	/// <param name="restHandler"></param>
	public AuthenticationService(IErtisAuthOptions ertisAuthOptions, ISystemRestHandler restHandler) : base(ertisAuthOptions, restHandler)
	{
		
	}
	
	#endregion
	
	#region Methods
	
	public async Task<IResponseResult<BearerToken>> GetTokenAsync(string username, string password, string? ipAddress = null, string? userAgent = null, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/generate-token";
		var headers = HeaderCollection.Add("Membership", this.MembershipId);
		if (!string.IsNullOrEmpty(ipAddress))
		{
			headers.Add("X-IpAddress", ipAddress);
		}
		
		if (!string.IsNullOrEmpty(userAgent))
		{
			headers.Add("X-UserAgent", userAgent);
		}
		
		var body = new
		{
			username,
			password
		};
		
		var response = await this.ExecuteRequestAsync(HttpMethod.Post, url, null, headers, new JsonRequestBody(body), cancellationToken: cancellationToken).ConfigureAwait(false);
		return ConvertToBearerTokenResponse(response);
	}
	
	public async Task<IResponseResult<BearerToken>> RefreshTokenAsync(BearerToken bearerToken, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrEmpty(bearerToken.RefreshToken))
		{
			throw ErtisAuthException.TokenIsNotRefreshable("Refresh token was missing");
		}
		
		var url = $"{this.BaseUrl}/refresh-token";
		var headers = HeaderCollection.Add("Authorization", bearerToken.RefreshToken);
		var response = await this.ExecuteRequestAsync(HttpMethod.Get, url, null, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
		return ConvertToBearerTokenResponse(response);
	}
	
	public async Task<IResponseResult<BearerToken>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/refresh-token";
		var headers = HeaderCollection.Add("Authorization", $"Bearer {refreshToken}");
		var response = await this.ExecuteRequestAsync(HttpMethod.Get, url, null, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
		return ConvertToBearerTokenResponse(response);
	}
	
	public async Task<IResponseResult<ITokenValidationResult>> VerifyTokenAsync(BearerToken token, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/verify-token";
		var headers = HeaderCollection.Add("Authorization", token.ToString());
		var response = await this.ExecuteRequestAsync<BearerTokenValidationResult>(HttpMethod.Get, url, null, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
		// ResponseResult<T> is not covariant; StatusCode can only be set through the constructor
		var result = response.StatusCode is { } statusCode
			? new ResponseResult<ITokenValidationResult>(statusCode)
			: new ResponseResult<ITokenValidationResult>(response.IsSuccess);
		
		result.Headers = response.Headers;
		result.Message = response.Message;
		// BearerTokenValidationResult is a struct; a failed response must not carry a boxed default value
		result.Data = response.IsSuccess ? response.Data : null;
		result.RawData = response.RawData;
		result.Json = response.Json;
		result.Exception = response.Exception;
		
		return result;
	}
	
	public async Task<IResponseResult<ITokenValidationResult>> VerifyTokenAsync(string accessToken, CancellationToken cancellationToken = default)
	{
		return await this.VerifyTokenAsync(BearerToken.CreateTemp(accessToken), cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult> RevokeTokenAsync(BearerToken token, bool logoutFromAllDevices = false, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/revoke-token";
		var headers = HeaderCollection.Add("Authorization", token.ToString());
		var queryString = logoutFromAllDevices ? QueryString.Add("logout-all", true) : QueryString.Empty;
		return await this.ExecuteRequestAsync(HttpMethod.Get, url, queryString, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult> RevokeTokenAsync(string accessToken, bool logoutFromAllDevices = false, CancellationToken cancellationToken = default)
	{
		return await this.RevokeTokenAsync(BearerToken.CreateTemp(accessToken), logoutFromAllDevices, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<User>> MeAsync(BearerToken bearerToken, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/me";
		var headers = HeaderCollection.Add("Authorization", bearerToken.ToString());
		return await this.ExecuteRequestAsync<User>(HttpMethod.Get, url, null, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<T>> MeAsync<T>(BearerToken bearerToken, CancellationToken cancellationToken = default) where T : class
	{
		var url = $"{this.BaseUrl}/me";
		var headers = HeaderCollection.Add("Authorization", bearerToken.ToString());
		return await this.ExecuteRequestAsync<T>(HttpMethod.Get, url, null, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<User>> WhoAmIAsync(BearerToken bearerToken, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/whoami";
		var headers = HeaderCollection.Add("Authorization", bearerToken.ToString());
		return await this.ExecuteRequestAsync<User>(HttpMethod.Get, url, null, headers, cancellationToken: cancellationToken).ConfigureAwait(false);	
	}
	
	public async Task<IResponseResult<T>> WhoAmIAsync<T>(BearerToken bearerToken, CancellationToken cancellationToken = default) where T : class
	{
		var url = $"{this.BaseUrl}/whoami";
		var headers = HeaderCollection.Add("Authorization", bearerToken.ToString());
		return await this.ExecuteRequestAsync<T>(HttpMethod.Get, url, null, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	public async Task<IResponseResult<Application>> WhoAmIAsync(BasicToken basicToken, CancellationToken cancellationToken = default)
	{
		var url = $"{this.BaseUrl}/whoami";
		var headers = HeaderCollection.Add("Authorization", basicToken.ToString());
		return await this.ExecuteRequestAsync<Application>(HttpMethod.Get, url, null, headers, cancellationToken: cancellationToken).ConfigureAwait(false);
	}
	
	private static IResponseResult<BearerToken> ConvertToBearerTokenResponse(IResponseResult response)
	{
		if (response is { IsSuccess: true, Json: not null })
		{
			var bearerToken = BearerToken.ParseFromJson(response.Json);
			if (response.StatusCode == null)
			{
				return new ResponseResult<BearerToken>(response.IsSuccess, response.Message ?? string.Empty)
				{
					Data = bearerToken,
					Json = response.Json,
					RawData = response.RawData,
					Exception = response.Exception
				};
			}
			else
			{
				return new ResponseResult<BearerToken>(response.StatusCode.Value, response.Message ?? string.Empty)
				{
					Data = bearerToken,
					Json = response.Json,
					RawData = response.RawData,
					Exception = response.Exception
				};
			}
		}
		else
		{
			if (response.StatusCode == null)
			{
				return new ResponseResult<BearerToken>(response.IsSuccess, response.Message ?? string.Empty)
				{
					Json = response.Json,
					RawData = response.RawData,
					Exception = response.Exception
				};
			}
			else
			{
				return new ResponseResult<BearerToken>(response.StatusCode.Value, response.Message ?? string.Empty)
				{
					Json = response.Json,
					RawData = response.RawData,
					Exception = response.Exception
				};
			}
		}
	}
	
	#endregion
}