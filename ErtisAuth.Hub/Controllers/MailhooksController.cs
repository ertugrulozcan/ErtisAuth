using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Memberships;
using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
    [RbacResource("mailhooks")]
    [Route("memberships/{membershipId}/mail-settings/mailhooks")]
    public class MailhooksController : Controller
    {
	    #region Services

	    private readonly IMailHookService mailHookService;
	    private readonly IAuthenticationService authenticationService;

	    #endregion

	    #region Constructors

	    /// <summary>
	    /// Constructor
	    /// </summary>
	    /// <param name="mailHookService"></param>
	    /// <param name="authenticationService"></param>
	    public MailhooksController(IMailHookService mailHookService, IAuthenticationService authenticationService)
	    {
		    this.mailHookService = mailHookService;
		    this.authenticationService = authenticationService;
	    }

	    #endregion
	    
		#region Create

		[HttpPost]
		[RbacAction(Rbac.CrudActions.Create)]
		public async Task<IActionResult> Create([FromForm] MailHookCreateViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				var token = this.GetBearerToken();
				var mailHook = new MailHook
				{
					Name = model.Name,
					Description = model.Description,
					Event = model.EventType,
					Status = model.IsActive ? "active" : "passive",
					MailSubject = model.MailSubject,
					MailTemplate = model.MailTemplate,
					FromName = model.FromName,
					FromAddress = model.FromAddress,
					MembershipId = model.MembershipId
				};
				
				var createMailHookResponse = await this.mailHookService.CreateAsync(mailHook, token);
				if (createMailHookResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Mailhook created";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("MailSettings", "Memberships", routeValues: new { id = createMailHookResponse.Data.MembershipId });
				}
				else
				{
					model.SetError(createMailHookResponse);
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("MailSettings", "Memberships", routeValues: new { id = model.MembershipId });
				}
			}
			else
			{
				model.IsSuccess = false;
				model.Errors = this.ModelState.Values.SelectMany(x => x.Errors.Select(y => y.ErrorMessage));
			}
			
			this.SetRedirectionParameter(new SerializableViewModel(model));
			return this.RedirectToAction("MailSettings", "Memberships", routeValues: new { id = model.MembershipId });
		}
		
		#endregion

		#region Detail

		[HttpGet("{id}")]
		[RbacObject("{id}")]
		public async Task<IActionResult> Detail([FromRoute] string membershipId, [FromRoute] string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return this.RedirectToAction("Index", "Memberships");
			}

			var token = this.GetBearerToken();
			var getMailHookResponse = await this.mailHookService.GetAsync(id, token);
			if (getMailHookResponse.IsSuccess)
			{
				var viewModel = new MailHookViewModel
				{
					Id = getMailHookResponse.Data.Id,
					Name = getMailHookResponse.Data.Name,
					Description = getMailHookResponse.Data.Description,
					EventType = getMailHookResponse.Data.Event,
					IsActive = getMailHookResponse.Data.IsActive,
					MailSubject = getMailHookResponse.Data.MailSubject,
					MailTemplate = getMailHookResponse.Data.MailTemplate,
					FromName = getMailHookResponse.Data.FromName,
					FromAddress = getMailHookResponse.Data.FromAddress,
					MembershipId = getMailHookResponse.Data.MembershipId,
					Sys = getMailHookResponse.Data.Sys
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
				var viewModel = new MailHookViewModel();
				viewModel.SetError(getMailHookResponse);
				return View(viewModel);
			}
		}

		#endregion
		
		#region Update

		[HttpPost("update")]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromForm] MailHookViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				var mailHook = new MailHook
				{
					Id = model.Id,
					Name = model.Name,
					Description = model.Description,
					Event = model.EventType,
					Status = model.IsActive ? "active" : "passive",
					MailSubject = model.MailSubject,
					MailTemplate = model.MailTemplate,
					FromName = model.FromName,
					FromAddress = model.FromAddress,
					MembershipId = model.MembershipId
				};

				var updateMailHookResponse = await this.mailHookService.UpdateAsync(mailHook, this.GetBearerToken());
				if (updateMailHookResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Mailhook updated";
				}
				else
				{
					model.SetError(updateMailHookResponse);
				}
			}
			else
			{
				model.IsSuccess = false;
				model.Errors = this.ModelState.Values.SelectMany(x => x.Errors.Select(y => y.ErrorMessage));
			}

			this.SetRedirectionParameter(new SerializableViewModel(model));
			return this.RedirectToAction("Detail", routeValues: new { membershipId = model.MembershipId, id = model.Id });
		}

		#endregion
		
		#region Delete

		[HttpPost("delete")]
		[RbacAction(Rbac.CrudActions.Delete)]
		public async Task<IActionResult> Delete([FromForm]DeleteViewModel deleteModel)
		{
			string membershipId = null;
			if (this.ModelState.IsValid)
			{
				var username = this.GetClaim(Claims.Username);
				var getTokenResponse = await this.authenticationService.GetTokenAsync(username, deleteModel.Password);
				if (getTokenResponse.IsSuccess)
				{
					var token = this.GetBearerToken();
					var getMailhookResponse = await this.mailHookService.GetAsync(deleteModel.ItemId, token);
					if (getMailhookResponse.IsSuccess)
					{
						membershipId = getMailhookResponse.Data.MembershipId;
						var deleteResponse = await this.mailHookService.DeleteAsync(deleteModel.ItemId, token);
						if (deleteResponse.IsSuccess)
						{
							this.SetRedirectionParameter(new SerializableViewModel
							{
								IsSuccess = true,
								SuccessMessage = "Mailhook deleted"
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
						model.SetError(getMailhookResponse);
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

			if (string.IsNullOrEmpty(membershipId))
			{
				return this.RedirectToAction("Index", "Memberships");	
			}
			else
			{
				return this.RedirectToAction("MailSettings", "Memberships", routeValues: new { id = membershipId });	
			}
		}

		#endregion
    }
}