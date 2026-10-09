using System.Threading.Tasks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Helpers;
using ErtisAuth.Extensions.AspNetCore.Models;
using ErtisAuth.Extensions.Http.Extensions;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ErtisAuth.Extensions.AspNetCore.Services;

internal class BearerAuthorizationHandler : IAuthorizationHandler<BearerToken>
{
	#region Services

	private readonly IAuthenticationService _authenticationService;
	private readonly IRoleService _roleService;
		
	#endregion
	
	#region Constructors
		
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="authenticationService"></param>
	/// <param name="roleService"></param>
	public BearerAuthorizationHandler(IAuthenticationService authenticationService, IRoleService roleService)
	{
		this._authenticationService = authenticationService;
		this._roleService = roleService;
	}
		
	#endregion
	
	#region Methods

	public async Task<AuthorizationResult> CheckAuthorizationAsync(BearerToken token, HttpContext context)
	{
		var meResponse = await this._authenticationService.WhoAmIAsync(token);
		if (meResponse.IsSuccess)
		{
			var rbacDefinition = context.GetRbacDefinition(meResponse.Data.Id);
			var rbac = rbacDefinition.ToString();
			var isPermittedForAction = await this._roleService.CheckPermissionAsync(rbac, token);
			if (!isPermittedForAction)
			{
				throw ErtisAuthException.AccessDenied($"You don't have permission to perform this action. Rbac: {rbac} (Error Code: 4033)");
			}
				
			Utilizer utilizer = meResponse.Data;
			utilizer.Token = token.AccessToken;
			utilizer.TokenType = SupportedTokenTypes.Bearer;
						
			return new AuthorizationResult(utilizer, rbacDefinition, true);
		}
		else
		{
			var errorMessage = meResponse.Message;
			if (ResponseHelper.TryParseError(meResponse.Message, out var error))
			{
				errorMessage = error.Message;
			}
						
			throw ErtisAuthException.Unauthorized(errorMessage);
		}
	}

	#endregion
}