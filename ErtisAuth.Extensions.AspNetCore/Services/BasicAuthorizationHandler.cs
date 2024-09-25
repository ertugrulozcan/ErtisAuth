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

	public async Task<AuthorizationResult> CheckAuthorizationAsync(BasicToken token, HttpContext context)
	{
		var applicationId = token.AccessToken.Split(':')[0];
		var rbacDefinition = context.GetRbacDefinition(applicationId);
		var rbac = rbacDefinition.ToString();
		if (this._configuration.BasicTokenCacheTTL is > 0 && this._memoryCache.TryGetValue<CacheEntry>(rbac, out var entry) && entry is  { Utilizer: not null })
		{
			this._logger.LogInformation("Basic token found on the cache ({Rbac})", rbac);
			return new AuthorizationResult(entry.Utilizer.Value, rbacDefinition, entry.IsAuthorized);
		}
		else
		{
			this._logger.LogInformation("Basic token not found on the cache ({Rbac}) Requesting from auth API", rbac);
			
			var getApplicationResponse = await this._applicationService.GetAsync(applicationId, token);
			if (getApplicationResponse.IsSuccess)
			{
				var isPermittedForAction = await this._roleService.CheckPermissionAsync(rbac, token);
				Utilizer utilizer = getApplicationResponse.Data;
				utilizer.Token = token.AccessToken;
				utilizer.TokenType = SupportedTokenTypes.Basic;

				if (this._configuration.BasicTokenCacheTTL is > 0)
				{
					var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(this._configuration.BasicTokenCacheTTL.Value));
					this._memoryCache.Set(rbac, new CacheEntry
					{
						Utilizer = utilizer,
						IsAuthorized = isPermittedForAction
					}, cacheOptions);
				}
				
				return new AuthorizationResult(utilizer, rbacDefinition, isPermittedForAction);
			}
			else
			{
				var errorMessage = getApplicationResponse.Message;
				if (ResponseHelper.TryParseError(getApplicationResponse.Message, out var error))
				{
					errorMessage = error.Message;
				}

				if (this._configuration.BasicTokenCacheTTL is > 0)
				{
					var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(this._configuration.BasicTokenCacheTTL.Value));
					this._memoryCache.Set(rbac, new CacheEntry
					{
						Utilizer = default,
						IsAuthorized = false
					}, cacheOptions);
				}
				
				throw ErtisAuthException.Unauthorized(errorMessage);
			}
		}
	}

	#endregion

	#region Cache Methods

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

		public Utilizer? Utilizer { get; init; }
		
		public bool IsAuthorized { get; init; }

		#endregion
	}
	
	#endregion
}