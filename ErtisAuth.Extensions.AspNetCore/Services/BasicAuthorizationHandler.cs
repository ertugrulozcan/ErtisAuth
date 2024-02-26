using System.Threading.Tasks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Helpers;
using ErtisAuth.Extensions.AspNetCore.Models;
using ErtisAuth.Extensions.Http.Extensions;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ErtisAuth.Extensions.AspNetCore.Services;

internal class BasicAuthorizationHandler : IAuthorizationHandler<BasicToken>
{
	#region Services
	
	private readonly IApplicationService _applicationService;
	private readonly IRoleService _roleService;
	
	#endregion
	
	#region Constructors
		
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="applicationService"></param>
	/// <param name="roleService"></param>
	public BasicAuthorizationHandler(IApplicationService applicationService, IRoleService roleService)
	{
		this._applicationService = applicationService;
		this._roleService = roleService;
	}
		
	#endregion
	
	#region Methods

	public async Task<AuthorizationResult> CheckAuthorizationAsync(BasicToken token, HttpContext context)
	{
		var applicationId = token.AccessToken.Split(':')[0];
		var rbacDefinition = context.GetRbacDefinition(applicationId);
		var rbac = rbacDefinition.ToString();
		var getApplicationResponse = await this._applicationService.GetAsync(applicationId, token);
		if (getApplicationResponse.IsSuccess)
		{
			var isPermittedForAction = await this._roleService.CheckPermissionAsync(rbac, token);
			Utilizer utilizer = getApplicationResponse.Data;
			utilizer.Token = token.AccessToken;
			utilizer.TokenType = SupportedTokenTypes.Basic;
						
			return new AuthorizationResult(utilizer, rbacDefinition, isPermittedForAction);
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

	#endregion
}