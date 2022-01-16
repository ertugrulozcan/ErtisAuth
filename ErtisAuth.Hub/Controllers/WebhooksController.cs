using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Webhooks;
using Newtonsoft.Json;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
    [RbacResource("webhooks")]
    [Route("webhooks")]
    public class WebhooksController : Controller
    {
        #region Services

		private readonly IWebhookService webhookService;
		private readonly IAuthenticationService authenticationService;

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="webhookService"></param>
		/// <param name="authenticationService"></param>
		public WebhooksController(
			IWebhookService webhookService, 
			IAuthenticationService authenticationService)
		{
			this.webhookService = webhookService;
			this.authenticationService = authenticationService;
		}

		#endregion
		
		#region Index

		[HttpGet]
		public IActionResult Index()
		{
			var viewModel = new WebhooksViewModel
			{
				CreateViewModel = new WebhookCreateViewModel
				{
					TryCount = 1
				}
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
		public async Task<IActionResult> Create([FromForm] WebhookCreateViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				if (!model.TryGetHeaders(out var headers, out var ex1))
				{
					model.IsSuccess = false;
					model.ErrorMessage = "Headers invalid";
					model.Errors = new[] { ex1.Message };
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");	
				}

				if (!model.TryGetBody(out var body, out var ex2))
				{
					model.IsSuccess = false;
					model.ErrorMessage = "Body invalid";
					model.Errors = new[] { ex2.Message };
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");
				}

				var requestList = new[]
				{
					new WebhookRequest
					{
						Url = model.RequestUrl,
						Method = model.RequestMethod,
						Headers = headers,
						Body = body
					}
				};
				
				var webhook = new Webhook
				{
					Name = model.Name,
					Description = model.Description,
					Event = model.EventType,
					Status = model.IsActive ? "active" : "passive",
					RequestList = requestList,
					TryCount = model.TryCount
				};

				var createWebhookResponse = await this.webhookService.CreateAsync(webhook, this.GetBearerToken());
				if (createWebhookResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Webhook created";
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Detail", routeValues: new { id = createWebhookResponse.Data.Id });
				}
				else
				{
					model.SetError(createWebhookResponse);
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
			var getWebhookResponse = await this.webhookService.GetAsync(id, token);
			if (getWebhookResponse.IsSuccess)
			{
				var webhookRequests = getWebhookResponse.Data.RequestList;
				var webhookRequest = webhookRequests.FirstOrDefault();
				var requestBody = string.Empty;
				if (webhookRequest?.Body != null)
				{
					requestBody = JsonConvert.SerializeObject(webhookRequest.Body, Formatting.Indented);
				}

				var viewModel = new WebhookViewModel
				{
					Id = getWebhookResponse.Data.Id,
					Name = getWebhookResponse.Data.Name,
					Description = getWebhookResponse.Data.Description,
					IsActive = getWebhookResponse.Data.IsActive,
					EventType = getWebhookResponse.Data.Event,
					TryCount = getWebhookResponse.Data.TryCount,
					RequestUrl = webhookRequest?.Url,
					RequestMethod = webhookRequest?.Method,
					RequestHeaders = webhookRequest?.Headers,
					RequestBody = requestBody,
					Sys = getWebhookResponse.Data.Sys
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
				var viewModel = new WebhookViewModel();
				viewModel.SetError(getWebhookResponse);

				return View(viewModel);
			}
		}

		#endregion

		#region Update

		[HttpPost]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromForm] WebhookViewModel model)
		{
			if (this.ModelState.IsValid)
			{
				if (!model.TryGetHeaders(out var headers, out var ex1))
				{
					model.IsSuccess = false;
					model.ErrorMessage = "Headers invalid";
					model.Errors = new[] { ex1.Message };
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");	
				}

				if (!model.TryGetBody(out var body, out var ex2))
				{
					model.IsSuccess = false;
					model.ErrorMessage = "Body invalid";
					model.Errors = new[] { ex2.Message };
					this.SetRedirectionParameter(new SerializableViewModel(model));
					return this.RedirectToAction("Index");
				}

				var requestList = new[]
				{
					new WebhookRequest
					{
						Url = model.RequestUrl,
						Method = model.RequestMethod,
						Headers = headers,
						Body = body
					}
				};
				
				var webhook = new Webhook
				{
					Id = model.Id,
					Name = model.Name,
					Description = model.Description,
					Event = model.EventType,
					Status = model.IsActive ? "active" : "passive",
					RequestList = requestList,
					TryCount = model.TryCount
				};

				var updateWebhookResponse = await this.webhookService.UpdateAsync(webhook, this.GetBearerToken());
				if (updateWebhookResponse.IsSuccess)
				{
					model.IsSuccess = true;
					model.SuccessMessage = "Webhook updated";
				}
				else
				{
					model.SetError(updateWebhookResponse);
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
		public async Task<IActionResult> Delete([FromForm]DeleteViewModel deleteWebhookModel)
		{
			if (this.ModelState.IsValid)
			{
				var username = this.GetClaim(Claims.Username);
				var getTokenResponse = await this.authenticationService.GetTokenAsync(username, deleteWebhookModel.Password);
				if (getTokenResponse.IsSuccess)
				{
					var deleteResponse = await this.webhookService.DeleteAsync(deleteWebhookModel.ItemId, this.GetBearerToken());
					if (deleteResponse.IsSuccess)
					{
						this.SetRedirectionParameter(new SerializableViewModel
						{
							IsSuccess = true,
							SuccessMessage = "Webhook deleted"
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