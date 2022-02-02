using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Applications;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
	[RbacResource("applications")]
	[Route("applications")]
	public class ApplicationsController : Controller
	{
		#region Services

		private readonly IApplicationService applicationService;
		private readonly IRoleService roleService;
		private readonly IMembershipService membershipService;
		private readonly IAuthenticationService authenticationService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="applicationService"></param>
		/// <param name="roleService"></param>
		/// <param name="membershipService"></param>
		/// <param name="authenticationService"></param>
		public ApplicationsController(
			IApplicationService applicationService, 
			IRoleService roleService,
			IMembershipService membershipService,
			IAuthenticationService authenticationService)
		{
			this.applicationService = applicationService;
			this.roleService = roleService;
			this.membershipService = membershipService;
			this.authenticationService = authenticationService;
		}

		#endregion
		
		#region Index

		[HttpGet]
		public async Task<IActionResult> Index()
		{
			var viewModel = new ApplicationsViewModel
			{
				CreateViewModel = await this.GetApplicationCreateViewModelAsync()
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
		public async Task<IActionResult> Create([FromForm] ApplicationCreateViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				var application = new Application
				{
					Name = model.Name,
					Role = model.Role
				};

				var createApplicationResponse = await this.applicationService.CreateAsync(application, this.GetBearerToken());
				if (createApplicationResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Application created";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Detail", routeValues: new { id = createApplicationResponse.Data.Id });
				}
				else
				{
					model.SetError(createApplicationResponse);
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");
				}
			}
			else
			{
				model.IsSuccess = false;
				model.Errors = this.ModelState.Values.SelectMany(x => x.Errors.Select(y => y.ErrorMessage));
			}
			
			this.SetRedirectionParameter(new SerializableViewModel(model));
			return this.RedirectToAction("Index");
		}
		
		private async Task<ApplicationCreateViewModel> GetApplicationCreateViewModelAsync(ApplicationCreateViewModel currentModel = null)
		{
			var model = currentModel ?? new ApplicationCreateViewModel();

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

			return model;
		}

		#endregion
		
		#region Detail

		[HttpGet("{id}")]
		[RbacObject("{id}")]
		public async Task<IActionResult> Detail(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return this.RedirectToAction("Index");
			}
			
			var token = this.GetBearerToken();
			var getApplicationResponse = await this.applicationService.GetAsync(id, token);
			if (getApplicationResponse.IsSuccess)
			{
				var getRolesResponse = await roleService.GetAsync(token);
				if (getRolesResponse.IsSuccess)
				{
					string basicToken = null;
					var getMembershipResponse = await this.membershipService.GetMembershipAsync(getApplicationResponse.Data.MembershipId, token);
					if (getMembershipResponse.IsSuccess)
					{
						basicToken = $"{getApplicationResponse.Data.Id}:{getMembershipResponse.Data.SecretKey}";
					}
					
					var roleList = getRolesResponse.Data.Items;
					var viewModel = new ApplicationViewModel
					{
						Id = getApplicationResponse.Data.Id,
						Name = getApplicationResponse.Data.Name,
						Role = getApplicationResponse.Data.Role,
						BasicToken = basicToken,
						Sys = getApplicationResponse.Data.Sys,
						RoleList = roleList.Select(x => new SelectListItem
						{
							Value = x.Name,
							Text = x.Name,
							Selected = x.Name == getApplicationResponse.Data.Role
						}).ToList()
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
					var viewModel = new ApplicationViewModel
					{
						Id = getApplicationResponse.Data.Id,
						Name = getApplicationResponse.Data.Name,
						Role = getApplicationResponse.Data.Role,
						Sys = getApplicationResponse.Data.Sys,
						IsSuccess = false,
						ErrorMessage = "Role list could not fetched!"
					};

					return View(viewModel);
				}	
			}
			else
			{
				var viewModel = new ApplicationViewModel();
				viewModel.SetError(getApplicationResponse);

				return View(viewModel);
			}
		}

		#endregion

		#region Update

		[HttpPost]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromForm] ApplicationViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				var application = new Application
				{
					Id = model.Id,
					Name = model.Name,
					Role = model.Role
				};

				var updateApplicationResponse = await this.applicationService.UpdateAsync(application, this.GetBearerToken());
				if (updateApplicationResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Application updated";
				}
				else
				{
					model.SetError(updateApplicationResponse);
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
		public async Task<IActionResult> Delete([FromForm]DeleteViewModel deleteApplicationModel)
		{
			if (this.ModelState.IsValid)
			{
				var username = this.GetClaim(Claims.Username);
				var getTokenResponse = await this.authenticationService.GetTokenAsync(username, deleteApplicationModel.Password);
				if (getTokenResponse.IsSuccess)
				{
					var deleteResponse = await this.applicationService.DeleteAsync(deleteApplicationModel.ItemId, this.GetBearerToken());
					if (deleteResponse.IsSuccess)
					{
						this.SetRedirectionParameter(new SerializableViewModel
						{
							IsSuccess = true,
							SuccessMessage = "Application deleted"
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
	}
}