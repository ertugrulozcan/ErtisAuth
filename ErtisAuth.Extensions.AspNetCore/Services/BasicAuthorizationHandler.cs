using System;
using System.Threading.Tasks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Helpers;
using ErtisAuth.Extensions.AspNetCore.Models;
using ErtisAuth.Extensions.Http.Extensions;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Extensions.AspNetCore.Services;

internal class BasicAuthorizationHandler : IAuthorizationHandler<BasicToken>
{
	#region Services
	
	private readonly IApplicationService _applicationService;
	private readonly IRoleService _roleService;
	private readonly IMemoryCache _memoryCache;
	private readonly IErtisAuthOptions _configuration;
	private readonly ILogger<BasicAuthorizationHandler> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="applicationService"></param>
	/// <param name="roleService"></param>
	/// <param name="memoryCache"></param>
	/// <param name="configuration"></param>
	/// <param name="logger"></param>
	public BasicAuthorizationHandler(
		IApplicationService applicationService, 
		IRoleService roleService, 
		IMemoryCache memoryCache,
		IErtisAuthOptions configuration,
		ILogger<BasicAuthorizationHandler> logger)
	{
		this._applicationService = applicationService;
		this._roleService = roleService;
		this._memoryCache = memoryCache;
		this._configuration = configuration;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	public async Task<Utilizer> CheckAuthenticationAsync(BasicToken token)
	{
		var applicationId = token.AccessToken.Split(':')[0];
		var getApplicationResponse = await this._applicationService.GetAsync(applicationId, token);
		if (getApplicationResponse.IsSuccess)
		{
			Utilizer utilizer = getApplicationResponse.Data;
			utilizer.Token = token.AccessToken;
			utilizer.TokenType = SupportedTokenTypes.Basic;
			return utilizer;
		}
		else
		{
			var errorMessage = getApplicationResponse.Message;
			if (ResponseHelper.TryParseError(getApplicationResponse.Message, out var error))
			{
				errorMessage = error.Message;
			}
			
			if (string.IsNullOrEmpty(errorMessage))
			{
				if (getApplicationResponse.Exception != null)
				{
					this._logger.LogError(getApplicationResponse.Exception, "An error occured on basic authorization check");
				}
				else
				{
					this._logger.LogError("An error occured on basic authorization check: {Response}", getApplicationResponse.Json);
				}
			}
			
			throw ErtisAuthException.Unauthorized(string.IsNullOrEmpty(errorMessage) ? "An error occured on basic authorization check" : errorMessage);
		}
	}
	
	public async Task<AuthorizationResult> CheckAuthorizationAsync(BasicToken token, HttpContext context)
	{
		var applicationId = token.AccessToken.Split(':')[0];
		var rbacDefinition = context.GetRbacDefinition(applicationId);
		var rbac = rbacDefinition.ToString();
		var cacheEntry = this.GetCacheEntry(token, rbac);
		if (cacheEntry is { Utilizer: not null })
		{
			Console.WriteLine($"Basic token found on the cache ({rbac})");
			return new AuthorizationResult(cacheEntry.Utilizer.Value, rbacDefinition, cacheEntry.IsAuthorized);
		}
		else
		{
			Console.WriteLine($"Basic token not found on the cache ({rbac}) Requesting from auth API");
			
			var getApplicationResponse = await this._applicationService.GetAsync(applicationId, token);
			if (getApplicationResponse.IsSuccess)
			{
				var isPermittedForAction = await this._roleService.CheckPermissionAsync(rbac, token);
				Utilizer utilizer = getApplicationResponse.Data;
				utilizer.Token = token.AccessToken;
				utilizer.TokenType = SupportedTokenTypes.Basic;
				this.SetCacheEntry(token, rbac, utilizer, isPermittedForAction);
				return new AuthorizationResult(utilizer, rbacDefinition, isPermittedForAction);
			}
			else
			{
				var errorMessage = getApplicationResponse.Message;
				if (ResponseHelper.TryParseError(getApplicationResponse.Message, out var error))
				{
					errorMessage = error.Message;
				}
				
				this.SetCacheEntry(token, rbac, null, false);
				
				if (string.IsNullOrEmpty(errorMessage))
				{
					if (getApplicationResponse.Exception != null)
					{
						this._logger.LogError(getApplicationResponse.Exception, "An error occured on basic authorization check");
					}
					else
					{
						this._logger.LogError("An error occured on basic authorization check: {Response}", getApplicationResponse.Json);
					}
				}
				
				throw ErtisAuthException.Unauthorized(string.IsNullOrEmpty(errorMessage) ? "An error occured on basic authorization check" : errorMessage);
			}
		}
	}
	
	#endregion
	
	#region Cache Methods
	
	private string GetCacheKey(BasicToken token, string rbac)
	{
		return $"{token.AccessToken}&{rbac}";
	}
	
	private CacheEntry GetCacheEntry(BasicToken token, string rbac)
	{
		if (this._configuration.BasicTokenCacheTTL is > 0)
		{
			var cacheKey = this.GetCacheKey(token, rbac);
			if (this._memoryCache.TryGetValue<CacheEntry>(cacheKey, out var entry) && entry is { Utilizer: not null })
			{
				return entry;
			}
		}
		
		return null;
	}
	
	private void SetCacheEntry(BasicToken token, string rbac, Utilizer? utilizer, bool isPermitted)
	{
		if (this._configuration.BasicTokenCacheTTL is > 0)
		{
			var cacheKey = this.GetCacheKey(token, rbac);
			var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(this._configuration.BasicTokenCacheTTL.Value));
			this._memoryCache.Set(cacheKey, new CacheEntry
			{
				Token = token.AccessToken,
				Utilizer = utilizer,
				IsAuthorized = isPermitted
			}, cacheOptions);
		}
	}
	
	// ReSharper disable once UnusedMember.Local
	private void PurgeAllCache()
	{
		if (this._memoryCache is MemoryCache concreteMemoryCache)
		{
			concreteMemoryCache.Clear();
		}
	}
	
	#endregion
	
	#region Helper Classes
	
	private class CacheEntry
	{
		#region Properties
		
		// ReSharper disable once UnusedAutoPropertyAccessor.Local
		public string Token { get; set; }
		
		public Utilizer? Utilizer { get; init; }
		
		public bool IsAuthorized { get; init; }
		
		#endregion
	}
	
	#endregion
}