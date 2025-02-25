using System.Threading.Tasks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Extensions.AspNetCore.Helpers;
using ErtisAuth.Extensions.AspNetCore.Models;
using ErtisAuth.Extensions.Http.Extensions;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Extensions.AspNetCore.Services;

internal class BearerAuthorizationHandler : IAuthorizationHandler<BearerToken>
{
	#region Services
	
	private readonly IAuthenticationService _authenticationService;
	private readonly IRoleService _roleService;
	private readonly ILogger<BearerAuthorizationHandler> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="authenticationService"></param>
	/// <param name="roleService"></param>
	public BearerAuthorizationHandler(IAuthenticationService authenticationService, IRoleService roleService, ILogger<BearerAuthorizationHandler> logger)
	{
		this._authenticationService = authenticationService;
		this._roleService = roleService;
		this._logger = logger;
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
			
			if (string.IsNullOrEmpty(errorMessage))
			{
				if (meResponse.Exception != null)
				{
					this._logger.LogError(meResponse.Exception, "An error occured on bearer authorization check");
				}
				else
				{
					// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
					this._logger.LogError(meResponse.Json ?? "No rew data", "An error occured on bearer authorization check");
				}
			}
			
			throw ErtisAuthException.Unauthorized(string.IsNullOrEmpty(errorMessage) ? "An error occured on bearer authorization handler" : errorMessage);
		}
	}
	
	#endregion
}