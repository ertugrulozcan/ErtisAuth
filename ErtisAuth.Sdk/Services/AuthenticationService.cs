using System;
using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;
using Newtonsoft.Json.Linq;

namespace ErtisAuth.Sdk.Services
{
	public class AuthenticationService : MembershipBoundedService, IAuthenticationService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public AuthenticationService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
		
		#region Methods

		public IResponseResult<BearerToken> GetToken(string username, string password)
		{
			var url = $"{this.AuthApiBaseUrl}/generate-token";
			var headers = HeaderCollection.Add("X-Ertis-Alias", this.AuthApiMembershipId);
			var body = new
			{
				username,
				password
			};
			
			var response = this.ExecuteRequest(HttpMethod.Post, url, null, headers, new JsonRequestBody(body));
			return ConvertToBearerTokenResponse(response);
		}

		public async Task<IResponseResult<BearerToken>> GetTokenAsync(string username, string password)
		{
			var url = $"{this.AuthApiBaseUrl}/generate-token";
			var headers = HeaderCollection.Add("X-Ertis-Alias", this.AuthApiMembershipId);
			var body = new
			{
				username,
				password
			};
			
			var response = await this.ExecuteRequestAsync(HttpMethod.Post, url, null, headers, new JsonRequestBody(body));
			return ConvertToBearerTokenResponse(response);
		}

		public IResponseResult<BearerToken> RefreshToken(BearerToken bearerToken)
		{
			var url = $"{this.AuthApiBaseUrl}/refresh-token";
			var headers = HeaderCollection.Add("Authorization", bearerToken.RefreshToken);
			var response = this.ExecuteRequest(HttpMethod.Get, url, null, headers);
			return ConvertToBearerTokenResponse(response);
		}

		public async Task<IResponseResult<BearerToken>> RefreshTokenAsync(BearerToken bearerToken)
		{
			var url = $"{this.AuthApiBaseUrl}/refresh-token";
			var headers = HeaderCollection.Add("Authorization", bearerToken.RefreshToken);
			var response = await this.ExecuteRequestAsync(HttpMethod.Get, url, null, headers);
			return ConvertToBearerTokenResponse(response);
		}

		public IResponseResult<BearerToken> RefreshToken(string refreshToken)
		{
			var url = $"{this.AuthApiBaseUrl}/refresh-token";
			var headers = HeaderCollection.Add("Authorization", $"Bearer {refreshToken}");
			var response = this.ExecuteRequest(HttpMethod.Get, url, null, headers);
			return ConvertToBearerTokenResponse(response);
		}

		public async Task<IResponseResult<BearerToken>> RefreshTokenAsync(string refreshToken)
		{
			var url = $"{this.AuthApiBaseUrl}/refresh-token";
			var headers = HeaderCollection.Add("Authorization", $"Bearer {refreshToken}");
			var response = await this.ExecuteRequestAsync(HttpMethod.Get, url, null, headers);
			return ConvertToBearerTokenResponse(response);
		}

		public IResponseResult<ITokenValidationResult> VerifyToken(BearerToken token)
		{
			var url = $"{this.AuthApiBaseUrl}/verify-token";
			var headers = HeaderCollection.Add("Authorization", token.ToString());
			var response = this.ExecuteRequest(HttpMethod.Get, url, null, headers);
			if (response.IsSuccess)
			{
				if (Newtonsoft.Json.JsonConvert.DeserializeObject(response.Json) is JObject jObject)
				{
					var isVerified = jObject["verified"]?.Value<bool>();
					var access_token = jObject["token"]?.Value<string>();
					var remaining_time = jObject["remaining_time"]?.Value<int>();

					if (response.StatusCode != null)
					{
						return new ResponseResult<ITokenValidationResult>(response.StatusCode.Value, response.Message)
						{
							Data = new BearerTokenValidationResult(isVerified ?? true, access_token, null, TimeSpan.FromSeconds(remaining_time ?? 0)),
							Json = response.Json,
							RawData = response.RawData
						};
					}
					else
					{
						return new ResponseResult<ITokenValidationResult>(response.IsSuccess, response.Message)
						{
							Data = new BearerTokenValidationResult(isVerified ?? true, access_token, null, TimeSpan.FromSeconds(remaining_time ?? 0)),
							Json = response.Json,
							RawData = response.RawData
						};
					}
				}
				else
				{
					return new ResponseResult<ITokenValidationResult>(false, "Response success but could not deserialized!")
					{
						Json = response.Json,
						RawData = response.RawData
					};
				}
			}
			else
			{
				if (response.StatusCode != null)
				{
					return new ResponseResult<ITokenValidationResult>(response.StatusCode.Value, response.Message)
					{
						Json = response.Json,
						RawData = response.RawData
					};
				}
				else
				{
					return new ResponseResult<ITokenValidationResult>(response.IsSuccess, response.Message)
					{
						Json = response.Json,
						RawData = response.RawData
					};
				}
			}
		}

		public async Task<IResponseResult<ITokenValidationResult>> VerifyTokenAsync(BearerToken token)
		{
			var url = $"{this.AuthApiBaseUrl}/verify-token";
			var headers = HeaderCollection.Add("Authorization", token.ToString());
			var response = await this.ExecuteRequestAsync<BearerToken>(HttpMethod.Get, url, null, headers);
			if (response.IsSuccess)
			{
				if (Newtonsoft.Json.JsonConvert.DeserializeObject(response.Json) is JObject jObject)
				{
					var isVerified = jObject["verified"]?.Value<bool>();
					var access_token = jObject["token"]?.Value<string>();
					var remaining_time = jObject["remaining_time"]?.Value<int>();

					if (response.StatusCode != null)
					{
						return new ResponseResult<ITokenValidationResult>(response.StatusCode.Value, response.Message)
						{
							Data = new BearerTokenValidationResult(isVerified ?? true, access_token, null, TimeSpan.FromSeconds(remaining_time ?? 0)),
							Json = response.Json,
							RawData = response.RawData
						};
					}
					else
					{
						return new ResponseResult<ITokenValidationResult>(response.IsSuccess, response.Message)
						{
							Data = new BearerTokenValidationResult(isVerified ?? true, access_token, null, TimeSpan.FromSeconds(remaining_time ?? 0)),
							Json = response.Json,
							RawData = response.RawData
						};
					}
				}
				else
				{
					return new ResponseResult<ITokenValidationResult>(false, "Response success but could not deserialized!")
					{
						Json = response.Json,
						RawData = response.RawData
					};
				}
			}
			else
			{
				if (response.StatusCode != null)
				{
					return new ResponseResult<ITokenValidationResult>(response.StatusCode.Value, response.Message)
					{
						Json = response.Json,
						RawData = response.RawData
					};
				}
				else
				{
					return new ResponseResult<ITokenValidationResult>(response.IsSuccess, response.Message)
					{
						Json = response.Json,
						RawData = response.RawData
					};
				}
			}
		}

		public IResponseResult<ITokenValidationResult> VerifyToken(string accessToken)
		{
			return this.VerifyToken(BearerToken.CreateTemp(accessToken));
		}

		public async Task<IResponseResult<ITokenValidationResult>> VerifyTokenAsync(string accessToken)
		{
			return await this.VerifyTokenAsync(BearerToken.CreateTemp(accessToken));
		}

		public IResponseResult RevokeToken(BearerToken token)
		{
			var url = $"{this.AuthApiBaseUrl}/revoke-token";
			var headers = HeaderCollection.Add("Authorization", token.ToString());
			return this.ExecuteRequest(HttpMethod.Get, url, null, headers);
		}

		public async Task<IResponseResult> RevokeTokenAsync(BearerToken token)
		{
			var url = $"{this.AuthApiBaseUrl}/revoke-token";
			var headers = HeaderCollection.Add("Authorization", token.ToString());
			return await this.ExecuteRequestAsync(HttpMethod.Get, url, null, headers);
		}

		public IResponseResult RevokeToken(string accessToken)
		{
			return this.RevokeToken(BearerToken.CreateTemp(accessToken));
		}

		public async Task<IResponseResult> RevokeTokenAsync(string accessToken)
		{
			return await this.RevokeTokenAsync(BearerToken.CreateTemp(accessToken));
		}

		public IResponseResult<User> WhoAmI(BearerToken bearerToken)
		{
			var url = $"{this.AuthApiBaseUrl}/whoami";
			var headers = HeaderCollection.Add("Authorization", bearerToken.ToString());
			return this.ExecuteRequest<User>(HttpMethod.Get, url, null, headers);	
		}

		public async Task<IResponseResult<User>> WhoAmIAsync(BearerToken bearerToken)
		{
			var url = $"{this.AuthApiBaseUrl}/whoami";
			var headers = HeaderCollection.Add("Authorization", bearerToken.ToString());
			return await this.ExecuteRequestAsync<User>(HttpMethod.Get, url, null, headers);	
		}

		public IResponseResult<Application> WhoAmI(BasicToken basicToken)
		{
			var url = $"{this.AuthApiBaseUrl}/whoami";
			var headers = HeaderCollection.Add("Authorization", basicToken.ToString());
			return this.ExecuteRequest<Application>(HttpMethod.Get, url, null, headers);	
		}

		public async Task<IResponseResult<Application>> WhoAmIAsync(BasicToken basicToken)
		{
			var url = $"{this.AuthApiBaseUrl}/whoami";
			var headers = HeaderCollection.Add("Authorization", basicToken.ToString());
			return await this.ExecuteRequestAsync<Application>(HttpMethod.Get, url, null, headers);
		}
		
		private static IResponseResult<BearerToken> ConvertToBearerTokenResponse(IResponseResult response)
		{
			if (response.IsSuccess)
			{
				var bearerToken = BearerToken.ParseFromJson(response.Json);
				if (response.StatusCode == null)
				{
					return new ResponseResult<BearerToken>(response.IsSuccess, response.Message)
					{
						Data = bearerToken,
						Json = response.Json,
						RawData = response.RawData,
						Exception = response.Exception
					};
				}
				else
				{
					return new ResponseResult<BearerToken>(response.StatusCode.Value, response.Message)
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
					return new ResponseResult<BearerToken>(response.IsSuccess, response.Message)
					{
						Json = response.Json,
						RawData = response.RawData,
						Exception = response.Exception
					};
				}
				else
				{
					return new ResponseResult<BearerToken>(response.StatusCode.Value, response.Message)
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
}