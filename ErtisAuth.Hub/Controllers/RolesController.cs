using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.Models;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Roles;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
	[RbacResource("roles")]
	[Route("roles")]
	public class RolesController : Controller
	{
		#region Services

		private readonly IRoleService roleService;
		private readonly IUserService userService;
		private readonly IApplicationService applicationService;
		private readonly IAuthenticationService authenticationService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="roleService"></param>
		/// <param name="userService"></param>
		/// <param name="applicationService"></param>
		/// <param name="authenticationService"></param>
		public RolesController(
			IRoleService roleService,
			IUserService userService,
			IApplicationService applicationService,
			IAuthenticationService authenticationService)
		{
			this.roleService = roleService;
			this.userService = userService;
			this.applicationService = applicationService;
			this.authenticationService = authenticationService;
		}

		#endregion
		
		#region Index

		[HttpGet]
		public IActionResult Index()
		{
			var viewModel = new RolesViewModel
			{
				CreateViewModel = this.GetRoleCreateViewModel()
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

		#region Create

		[HttpPost("create")]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromForm] RoleCreateViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				var permissionArray = model.ParsePermissionArray()?.ToArray();
				var forbiddenArray = model.ParseForbiddenArray()?.ToArray();
				model.Permissions = permissionArray;
				model.Forbidden = forbiddenArray;
				model.PermissionsForBasicTab = GetPermissionsForBasicTab(permissionArray);
				
				var role = new Role
				{
					Name = model.Name,
					Description = model.Description,
					Permissions = permissionArray?.Select(x => x.ToString()),
					Forbidden = forbiddenArray?.Select(x => x.ToString())
				};

				var accessToken = this.GetBearerToken();
				var rbacSubjectsValidationResults = await this.CheckRbacSubjectsAsync(role, permissionArray, forbiddenArray, accessToken);
				if (rbacSubjectsValidationResults.Any())
				{
					model.IsSuccess = false;
					model.ErrorMessage = "Please check the subject columns";
					model.Errors = rbacSubjectsValidationResults.Select(x => x.Message);
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");
				}
				
				var createRoleResponse = await this.roleService.CreateAsync(role, this.GetBearerToken());
				if (createRoleResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Role created";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Detail", routeValues: new { id = createRoleResponse.Data.Id });
				}
				else
				{
					model.SetError(createRoleResponse);
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");
				}
			}
			else
			{
				model.IsSuccess = false;
				model.Errors = this.ModelState.Values.SelectMany(x => x.Errors.Select(y => y.ErrorMessage));
			}
			
			return this.RedirectToAction("Index");
		}
		
		private RoleCreateViewModel GetRoleCreateViewModel()
		{
			var defaultPermissions = new []
			{
				new Rbac(RbacSegment.All, new RbacSegment("users"), Rbac.CrudActionSegments.Read, RbacSegment.All),
				new Rbac(RbacSegment.All, new RbacSegment("roles"), Rbac.CrudActionSegments.Read, RbacSegment.All),
				new Rbac(RbacSegment.All, new RbacSegment("tokens"), Rbac.CrudActionSegments.Create, RbacSegment.All),
				new Rbac(RbacSegment.All, new RbacSegment("tokens"), Rbac.CrudActionSegments.Read, RbacSegment.All)
			};
			
			var model = new RoleCreateViewModel
			{
				Permissions = defaultPermissions,
				PermissionsForBasicTab = GetPermissionsForBasicTab(defaultPermissions),
			};

			return model;
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
			var getRoleResponse = await this.roleService.GetAsync(id, token);
			if (getRoleResponse.IsSuccess)
			{
				var permissions = getRoleResponse.Data.Permissions?.Select(Rbac.Parse).ToArray();
				var viewModel = new RoleViewModel
				{
					Id = getRoleResponse.Data.Id,
					Name = getRoleResponse.Data.Name,
					Description = getRoleResponse.Data.Description,
					Permissions = permissions,
					Forbidden = getRoleResponse.Data.Forbidden?.Select(Rbac.Parse).ToArray(),
					PermissionsForBasicTab = GetPermissionsForBasicTab(permissions),
					Sys = getRoleResponse.Data.Sys
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
				var viewModel = new RoleViewModel();
				viewModel.SetError(getRoleResponse);

				return View(viewModel);
			}
		}

		#endregion
		
		#region Update

		[HttpPost]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromForm] RoleViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				var permissionArray = model.ParsePermissionArray()?.ToArray();
				var forbiddenArray = model.ParseForbiddenArray()?.ToArray();
				var role = new Role
				{
					Id = model.Id,
					Name = model.Name,
					Description = model.Description,
					Permissions = permissionArray?.Select(x => x.ToString()),
					Forbidden = forbiddenArray?.Select(x => x.ToString())
				};

				var accessToken = this.GetBearerToken();
				var rbacSubjectsValidationResults = await this.CheckRbacSubjectsAsync(role, permissionArray, forbiddenArray, accessToken);
				if (rbacSubjectsValidationResults.Any())
				{
					model.IsSuccess = false;
					model.ErrorMessage = "Please check the subject columns";
					model.Errors = rbacSubjectsValidationResults.Select(x => x.Message);
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Detail", routeValues: new { id = model.Id });
				}

				var updateRoleResponse = await this.roleService.UpdateAsync(role, accessToken);
				if (updateRoleResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Role updated";
				}
				else
				{
					model.SetError(updateRoleResponse);
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

		#endregion

		#region Delete

		[HttpPost("delete")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromForm]DeleteViewModel deleteUserModel)
		{
			if (this.ModelState.IsValid)
			{
				var username = this.GetClaim(Claims.Username);
				var getTokenResponse = await this.authenticationService.GetTokenAsync(username, deleteUserModel.Password);
				if (getTokenResponse.IsSuccess)
				{
					var deleteResponse = await this.roleService.DeleteAsync(deleteUserModel.ItemId, this.GetBearerToken());
					if (deleteResponse.IsSuccess)
					{
						this.SetRedirectionParameter(new SerializableViewModel
						{
							IsSuccess = true,
							SuccessMessage = "Role deleted"
						});
					}
					else
					{
						var model = new SerializableViewModel();
						model.SetError(deleteResponse);
						this.SetRedirectionParameter(model);
					}
				}
				else
				{
					var model = new SerializableViewModel();
					model.SetError(getTokenResponse);
					this.SetRedirectionParameter(model);
				}
			}
			
			return this.RedirectToAction("Index");
		}

		#endregion

		#region Other Methods

		private static Dictionary<RbacSegment, Rbac[]> GetPermissionsForBasicTab(IEnumerable<Rbac> permissions)
		{
			string[] predefinedResources = 
			{
				"users",
				"applications",
				"roles",
				"memberships",
				"events",
				"providers",
				"webhooks",
				"tokens"
			};
			
			var dictionary = new Dictionary<RbacSegment, Rbac[]>();
			var defaultsDictionary = permissions?.GroupBy(x => x.Resource).ToDictionary(x => x.Key, y => y.ToArray()) ?? new Dictionary<RbacSegment, Rbac[]>();
			foreach (var predefinedResource in predefinedResources)
			{
				var resourceSegment = new RbacSegment(predefinedResource);
				if (defaultsDictionary.ContainsKey(resourceSegment))
				{
					dictionary.Add(resourceSegment, defaultsDictionary[resourceSegment]);
				}
				else if (!dictionary.ContainsKey(resourceSegment))
				{
					dictionary.Add(resourceSegment, Array.Empty<Rbac>());	
				}
			}

			foreach (var (rbacSegment, rbacArray) in defaultsDictionary)
			{
				if (!dictionary.ContainsKey(rbacSegment))
				{
					dictionary.Add(rbacSegment, rbacArray);
				}
			}

			return dictionary;
		}

		// ReSharper disable once UnusedMember.Local
		private async Task<RbacSubjectCheckResult[]> CheckRbacSubjectsAsync(string roleId, IEnumerable<Rbac> permissionList, IEnumerable<Rbac> forbiddenList, TokenBase accessToken)
		{
			var getRoleResponse = await this.roleService.GetAsync(roleId, accessToken);
			if (getRoleResponse.IsSuccess)
			{
				var role = getRoleResponse.Data;
				return await this.CheckRbacSubjectsAsync(role, permissionList, forbiddenList, accessToken);
			}
			else
			{
				return new[]
				{
					new RbacSubjectCheckResult
					{
						Reason = RbacSubjectCheckResult.RbacSubjectRejectReason.RoleNotFound,
						RoleId = roleId,
						Message = ErtisAuthException.RoleNotFound(roleId).Message
					}
				};
			}
		}
		
		private async Task<RbacSubjectCheckResult[]> CheckRbacSubjectsAsync(Role role, IEnumerable<Rbac> permissionList, IEnumerable<Rbac> forbiddenList, TokenBase accessToken)
		{
			var errorList = new List<RbacSubjectCheckResult>();

			var rbacList = new List<Rbac>();
			if (permissionList != null)
			{
				rbacList.AddRange(permissionList);
			}
			
			if (forbiddenList != null)
			{
				rbacList.AddRange(forbiddenList);
			}

			var subjects = rbacList.Where(x => !x.Subject.IsAll()).Select(x => x.Subject).Distinct();
			foreach (var subject in subjects)
			{
				bool? isUserExist;
				bool? isUserFromThisRole = null;
				bool? isApplicationExist = null;
				bool? isApplicationFromThisRole = null;
				
				var getUserResponse = await this.userService.GetAsync(subject.Value, accessToken);
				if (getUserResponse.IsSuccess)
				{
					isUserExist = true;
					isUserFromThisRole = getUserResponse.Data.Role == role.Name;
				}
				else
				{
					isUserExist = false;
				}

				if (!isUserExist.Value)
				{
					var getApplicationResponse = await this.applicationService.GetAsync(subject.Value, accessToken);
					if (getApplicationResponse.IsSuccess)
					{
						isApplicationExist = true;
						isApplicationFromThisRole = getApplicationResponse.Data.Role == role.Name;
					}
					else
					{
						isApplicationExist = false;
					}
				}

				if (!isUserExist.Value && !isApplicationExist.Value)
				{
					// User or application not found!
					errorList.Add(new RbacSubjectCheckResult
					{
						Reason = RbacSubjectCheckResult.RbacSubjectRejectReason.UtilizerNotFound,
						RoleId = role.Id,
						UtilizerId = subject.Value,
						Message = $"User or application not found with given id <{subject.Value}>"
					});
				}
				else if (isUserFromThisRole != null && !isUserFromThisRole.Value)
				{
					// The user is not from this role!
					errorList.Add(new RbacSubjectCheckResult
					{
						Reason = RbacSubjectCheckResult.RbacSubjectRejectReason.OutOfRole,
						RoleId = role.Id,
						UtilizerId = subject.Value,
						UtilizerType = Utilizer.UtilizerType.User,
						UtilizerName = getUserResponse.Data?.Username,
						Message = $"The user with id <{subject.Value}> is not from this role ({getUserResponse.Data?.Username})"
					});
				}
				else if (isApplicationFromThisRole != null && !isApplicationFromThisRole.Value)
				{
					// The application is not from this role!
					errorList.Add(new RbacSubjectCheckResult
					{
						Reason = RbacSubjectCheckResult.RbacSubjectRejectReason.OutOfRole,
						RoleId = role.Id,
						UtilizerId = subject.Value,
						UtilizerType = Utilizer.UtilizerType.Application,
						UtilizerName = getUserResponse.Data?.Username,
						Message = $"The application with id <{subject.Value}> is not from this role ({getUserResponse.Data?.Username})"
					});
				}
			}

			return errorList.ToArray();
		}

		#endregion
	}
}