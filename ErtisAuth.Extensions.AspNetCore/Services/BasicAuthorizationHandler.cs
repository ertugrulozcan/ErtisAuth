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

namespace ErtisAuth.Extensions.AspNetCore.Services;

internal class BasicAuthorizationHandler : IAuthorizationHandler<BasicToken>
{
	#region Constants

	private static readonly TimeSpan DefaultCacheTTL = TimeSpan.FromMinutes(10);

	#endregion
	
	#region Services

	private readonly IErtisAuthOptions _ertisAuthOptions;
	private readonly IApplicationService _applicationService;
	private readonly IRoleService _roleService;
	private readonly IMemoryCache _memoryCache;
		
	#endregion
	
	#region Constructors
		
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="ertisAuthOptions"></param>
	/// <param name="applicationService"></param>
	/// <param name="roleService"></param>
	/// <param name="memoryCache"></param>
	public BasicAuthorizationHandler(
		IErtisAuthOptions ertisAuthOptions,
		IApplicationService applicationService,
		IRoleService roleService,
		IMemoryCache memoryCache)
	{
		this._ertisAuthOptions = ertisAuthOptions;
		this._applicationService = applicationService;
		this._roleService = roleService;
		this._memoryCache = memoryCache;
	}
		
	#endregion
	
	#region Methods

	public async Task<AuthorizationResult> CheckAuthorizationAsync(BasicToken token, HttpContext context)
	{
		var applicationId = token.AccessToken.Split(':')[0];
		var rbacDefinition = context.GetRbacDefinition(applicationId);
		var rbac = rbacDefinition.ToString();
		var cacheKey = $"{token}-{rbac}";
		if (this._memoryCache.TryGetValue<AuthorizationResult>(cacheKey, out var authorizationResultInCache))
		{
			return authorizationResultInCache;
		}
		else
		{
			var getApplicationResponse = await this._applicationService.GetAsync(applicationId, token);
			if (getApplicationResponse.IsSuccess)
			{
				var isPermittedForAction = await this._roleService.CheckPermissionAsync(rbac, token);
				Utilizer utilizer = getApplicationResponse.Data;
				utilizer.Token = token.AccessToken;
				utilizer.TokenType = SupportedTokenTypes.Basic;
						
				var authorizationResult = new AuthorizationResult(utilizer, rbacDefinition, isPermittedForAction);
				this._memoryCache.Set(cacheKey, authorizationResult, this._ertisAuthOptions.CacheTTL is > 0 ? TimeSpan.FromSeconds(this._ertisAuthOptions.CacheTTL.Value) : DefaultCacheTTL);
				
				return authorizationResult;
			}
			else
			{
				var errorMessage = getApplicationResponse.Message;
				if (ResponseHelper.TryParseError(getApplicationResponse.Message, out var error))
				{
					errorMessage = error.Message;
				}
						
				throw ErtisAuthException.Unauthorized(errorMessage);
			}
		}
	}

	#endregion
}