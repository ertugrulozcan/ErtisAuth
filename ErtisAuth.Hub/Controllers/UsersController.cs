using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Helpers;
using ErtisAuth.Hub.Models;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Users;
using ErtisAuth.Sdk.Configuration;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
	[RbacResource("users")]
	[Route("users")]
	public class UsersController : BaseController
	{
		#region Services

		private readonly IErtisAuthOptions ertisAuthOptions;
		private readonly IUserService userService;
		private readonly IRoleService roleService;
		private readonly IMembershipService membershipService;
		private readonly IAuthenticationService authenticationService;
		private readonly IPasswordService passwordService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="userService"></param>
		/// <param name="roleService"></param>
		/// <param name="membershipService"></param>
		/// <param name="authenticationService"></param>
		/// <param name="passwordService"></param>
		public UsersController(
			IErtisAuthOptions ertisAuthOptions,
			IUserService userService, 
			IRoleService roleService, 
			IMembershipService membershipService,
			IAuthenticationService authenticationService, 
			IPasswordService passwordService) : base(authenticationService, userService)
		{
			this.ertisAuthOptions = ertisAuthOptions;
			this.userService = userService;
			this.roleService = roleService;
			this.membershipService = membershipService;
			this.authenticationService = authenticationService;
			this.passwordService = passwordService;
		}

		#endregion
		
		#region Index

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var viewModel = new UsersViewModel
			{
				CreateViewModel = await this.GetUserCreateViewModelAsync()
			};
			
			var routedModel = this.GetRedirectionParameter<SerializableViewModel>();
			if (routedModel != null)
			{
				viewModel.IsSuccess = routedModel.IsSuccess;
				viewModel.ErrorMessage = routedModel.ErrorMessage;
				viewModel.SuccessMessage = routedModel.SuccessMessage;
				viewModel.Error = routedModel.Error;
				viewModel.Errors = routedModel.Errors;
			}
			
			return View(viewModel);
		}

		#endregion

		#region Detail

		[HttpGet("{id}")]
		public async Task<IActionResult> Detail(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return this.RedirectToAction("Index");
			}
			
			var token = this.GetBearerToken();
			var getUserResponse = await this.userService.GetAsync(id, token);
			if (getUserResponse.IsSuccess)
			{
				var getMembershipResponse = await this.membershipService.GetMembershipAsync(getUserResponse.Data.MembershipId, token);
				var getRolesResponse = await this.roleService.GetAsync(token);
				if (getRolesResponse.IsSuccess)
				{
					IEnumerable<ActiveToken> activeTokens = null;
					var getActiveTokensResponse = await this.userService.GetActiveTokensAsync(id, token);
					if (getActiveTokensResponse.IsSuccess)
					{
						activeTokens = getActiveTokensResponse.Data.Items.OrderByDescending(x => x.CreatedAt);
					}
					
					IEnumerable<RevokedToken> revokedTokens = null;
					var getRevokedTokensResponse = await this.userService.GetRevokedTokensAsync(id, token);
					if (getRevokedTokensResponse.IsSuccess)
					{
						revokedTokens = getRevokedTokensResponse.Data.Items.OrderByDescending(x => x.RevokedAt);
					}

					var roleList = getRolesResponse.Data.Items.ToList();
					var userRole = roleList.FirstOrDefault(x => x.Name == getUserResponse.Data.Role);
					UbacTable ubacTable = null;
					if (userRole != null)
					{
						var userPermissions = getUserResponse.Data.Permissions;
						var userForbidden = getUserResponse.Data.Forbidden;
						var rolePermissions = userRole.Permissions;
						var roleForbidden = userRole.Forbidden;
						
						ubacTable = UserPermissionsHelper.MergePermissionsAndForbiddens(id, userPermissions, userForbidden, rolePermissions, roleForbidden);
					}

					var viewModel = new UserViewModel
					{
						Id = getUserResponse.Data.Id,
						FirstName = getUserResponse.Data.FirstName,
						LastName = getUserResponse.Data.LastName,
						EmailAddress = getUserResponse.Data.EmailAddress,
						Username = getUserResponse.Data.Username,
						RoleId = userRole?.Id,
						Role = getUserResponse.Data.Role,
						MembershipId = getUserResponse.Data.MembershipId,
						Sys = getUserResponse.Data.Sys,
						ActiveTokens = activeTokens, 
						RevokedTokens = revokedTokens,
						RoleList = roleList.Select(x => new SelectListItem
						{
							Value = x.Name,
							Text = x.Name,
							Selected = x.Name == getUserResponse.Data.Role
						}).ToList(),
						UbacTable = ubacTable,
						UserType = getMembershipResponse.Data.UserType,
						AdditionalProperties = getUserResponse.Data.AdditionalProperties
					};

					var routedModel = this.GetRedirectionParameter<SerializableViewModel>();
					if (routedModel != null)
					{
						viewModel.IsSuccess = routedModel.IsSuccess;
						viewModel.ErrorMessage = routedModel.ErrorMessage;
						viewModel.SuccessMessage = routedModel.SuccessMessage;
						viewModel.Error = routedModel.Error;
						viewModel.Errors = routedModel.Errors;
					}
					
					return View(viewModel);
				}
				else
				{
					var viewModel = new UserViewModel
					{
						Id = getUserResponse.Data.Id,
						FirstName = getUserResponse.Data.FirstName,
						LastName = getUserResponse.Data.LastName,
						EmailAddress = getUserResponse.Data.EmailAddress,
						Username = getUserResponse.Data.Username,
						Role = getUserResponse.Data.Role,
						MembershipId = getUserResponse.Data.MembershipId,
						Sys = getUserResponse.Data.Sys,
						IsSuccess = false,
						ErrorMessage = "Role list could not fetched!"
					};

					return View(viewModel);
				}	
			}
			else
			{
				var viewModel = new UserViewModel();
				viewModel.SetError(getUserResponse);

				return View(viewModel);
			}
		}

		#endregion
		
		#region Create
		
		[HttpPost("create")]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromForm] UserCreateViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				if (model.Password != model.PasswordAgain)
				{
					model = await this.GetUserCreateViewModelAsync(model);
					model.IsSuccess = false;
					model.ErrorMessage = "Passwords mismatch";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");
				}
				
				var user = new UserWithPassword
				{
					FirstName = model.FirstName,
					LastName = model.LastName,
					Username = model.Username,
					EmailAddress = model.EmailAddress,
					Role = model.Role,
					Password = model.Password
				};

				var createUserResponse = await this.userService.CreateAsync(user, this.GetBearerToken());
				if (createUserResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "User created";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Detail", routeValues: new { id = createUserResponse.Data.Id });
				}
				else
				{
					model = await this.GetUserCreateViewModelAsync(model);
					model.SetError(createUserResponse);
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");
				}
			}
			else
			{
				model = await this.GetUserCreateViewModelAsync(model);
				model.IsSuccess = false;
				model.Errors = this.ModelState.Values.SelectMany(x => x.Errors.Select(y => y.ErrorMessage));
			}
			
			model = await this.GetUserCreateViewModelAsync(model);
			this.SetRedirectionParameter(new SerializableViewModel(model));
			return this.RedirectToAction("Index");
		}

		private async Task<UserCreateViewModel> GetUserCreateViewModelAsync(UserCreateViewModel currentModel = null)
		{
			var model = currentModel ?? new UserCreateViewModel();

			var token = this.GetBearerToken();
			var getRolesResponse = await this.roleService.GetAsync(token);
			if (getRolesResponse.IsSuccess)
			{
				var roleList = getRolesResponse.Data.Items;
				model.RoleList = roleList.Select(x => new SelectListItem
				{
					Value = x.Name,
					Text = x.Name
				}).ToList();
			}
			else
			{
				model.IsSuccess = false;
				model.ErrorMessage = "Role list could not fetched!";
			}
			
			var getMembershipResponse = await this.membershipService.GetMembershipAsync(this.ertisAuthOptions.MembershipId, token);
			if (getMembershipResponse.IsSuccess)
			{
				model.UserType = getMembershipResponse.Data.UserType;
			}
			
			return model;
		}

		#endregion
		
		#region Update

		[HttpPost("{id}")]
		[RbacAction(Rbac.CrudActions.Update)]
		[RbacObject("{id}")]
		public async Task<IActionResult> Update([FromRoute] string id, [FromForm] UserViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				var token = this.GetBearerToken();
				
				var getRoleResult = await this.roleService.GetAsync(model.RoleId, token);
				if (!getRoleResult.IsSuccess || getRoleResult.Data == null)
				{
					model.IsSuccess = false;
					model.ErrorMessage = "User role could not be fetched!";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Detail", routeValues: new { id = model.Id });
				}
				
				var mergedPermissions = model.ParsePermissionArray()?.ToArray();
				var mergedForbiddens = model.ParseForbiddenArray()?.ToArray();
				var (permissions, forbiddens) = CalculateDiffRoleAndUserPermissions(model.Id, getRoleResult.Data, mergedPermissions, mergedForbiddens);

				var user = new User
				{
					Id = model.Id,
					FirstName = model.FirstName,
					LastName = model.LastName,
					Username = model.Username,
					EmailAddress = model.EmailAddress,
					Role = model.Role,
					MembershipId = model.MembershipId,
					AdditionalProperties = model.AdditionalProperties,
					Permissions = permissions,
					Forbidden = forbiddens
				};

				var updateUserResponse = await this.userService.UpdateAsync(user, token);
				if (updateUserResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "User updated";
				}
				else
				{
					model.SetError(updateUserResponse);
				}
			}
			else
			{
				model.IsSuccess = false;
				model.Errors = this.ModelState.Values.SelectMany(x => x.Errors.Select(y => y.ErrorMessage));
			}

			this.SetRedirectionParameter(new SerializableViewModel(model));
			return this.RedirectToAction("Detail", routeValues: new { id = model.Id });
		}

		/// <summary>
		/// Calculate differency between as role permissions and user permissions
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="role"></param>
		/// <param name="mergedPermissions"></param>
		/// <param name="mergedForbiddens"></param>
		/// <returns>Tuple 1 permissions, Tuple 2 forbiddens</returns>
		private static Tuple<IEnumerable<string>, IEnumerable<string>> CalculateDiffRoleAndUserPermissions(string userId, Role role, ICollection<Ubac> mergedPermissions, ICollection<Ubac> mergedForbiddens)
		{
			var rolePermissions = role.Permissions?.Select(Rbac.Parse).ToArray();
			var roleForbiddens = role.Forbidden?.Select(Rbac.Parse).ToArray();
				
			var permissions1 = new List<Ubac>();
			var forbiddens1 = new List<Rbac>();
			if (mergedPermissions != null && rolePermissions != null)
			{
				foreach (var ubac in mergedPermissions)
				{
					if (!rolePermissions.IsExist(ubac.Resource, ubac.Action, ubac.Object, userId))
					{
						permissions1.Add(ubac);
					}
				}

				foreach (var rbac in rolePermissions)
				{
					if (!mergedPermissions.IsExist(rbac.Resource, rbac.Action, rbac.Object))
					{
						forbiddens1.Add(rbac);
					}
				}
			}
				
			var permissions2 = new List<Rbac>();
			var forbiddens2 = new List<Ubac>();
			if (mergedForbiddens != null && roleForbiddens != null)
			{
				foreach (var ubac in mergedForbiddens)
				{
					if (!roleForbiddens.IsExist(ubac.Resource, ubac.Action, ubac.Object, userId))
					{
						forbiddens2.Add(ubac);
					}
				}

				foreach (var rbac in roleForbiddens)
				{
					if (!mergedForbiddens.IsExist(rbac.Resource, rbac.Action, rbac.Object))
					{
						permissions2.Add(rbac);
					}
				}
			}

			var permissions = permissions1
				.Select(x => x.ToString())
				.Concat(permissions2.Select(x => x.AsUbac().ToString()))
				.Distinct();
				
			var forbiddens = forbiddens2
				.Select(x => x.ToString())
				.Concat(forbiddens1.Select(x => x.AsUbac().ToString()))
				.Distinct();

			return new Tuple<IEnumerable<string>, IEnumerable<string>>(permissions, forbiddens);
		}

		#endregion

		#region Change Password

		[HttpPost("change-password")]
		public async Task<IActionResult> ChangePassword([FromForm] ChangePasswordViewModel model)
		{
			var userId = this.GetClaim(Claims.UserId);
			
			if (this.ModelState.IsValid)
			{
				var currentPasswordCheckResponse = await this.authenticationService.GetTokenAsync(this.GetClaim(Claims.Username), model.CurrentPassword);
				if (!currentPasswordCheckResponse.IsSuccess)
				{
					model.IsSuccess = false;
					model.ErrorMessage = "Wrong password!";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Detail", routeValues: new { id = userId });
				}
				
				if (model.NewPassword != model.NewPasswordAgain)
				{
					model.IsSuccess = false;
					model.ErrorMessage = "Passwords mismatch";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Detail", routeValues: new { id = userId });
				}
				
				var changePasswordResponse = await this.passwordService.ChangePasswordAsync(userId, model.NewPassword, this.GetBearerToken()); 
				if (changePasswordResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Password changed";
				}
				else
				{
					model.SetError(changePasswordResponse);
				}
			}
			else
			{
				model.IsSuccess = false;
				model.Errors = this.ModelState.Values.SelectMany(x => x.Errors.Select(y => y.ErrorMessage));
			}

			this.SetRedirectionParameter(new SerializableViewModel(model));
			return this.RedirectToAction("Detail", routeValues: new { id = userId });
		}

		#endregion

		#region Revoke Token

		[HttpGet("revoke-token/{userId}/{tokenId}")]
		public async Task<IActionResult> RevokeToken([FromRoute] string userId, string tokenId)
		{
			var token = this.GetBearerToken();
			var getActiveTokensResponse = await this.userService.GetActiveTokensAsync(userId, token);
			if (getActiveTokensResponse.IsSuccess)
			{
				var activeTokens = getActiveTokensResponse.Data.Items;
				var activeToken = activeTokens.FirstOrDefault(x => x.Id == tokenId);
				if (activeToken != null)
				{
					await this.authenticationService.RevokeTokenAsync(activeToken.AccessToken);
				}
			}

			if (this.Request.Query.ContainsKey("returnUrl"))
			{
				var returnUrl = this.Request.Query["returnUrl"];
				return this.Redirect($"/{returnUrl}");
			}
			else
			{
				return this.RedirectToAction("Detail", routeValues: new { id = userId });	
			}
		}

		#endregion
	}
}