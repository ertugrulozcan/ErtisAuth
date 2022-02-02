using System;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Events;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
	[RbacResource("events")]
	[Route("events")]
	public class EventsController : Controller
	{
		#region Services

		private readonly IEventService eventService;
		private readonly IUserService userService;
		private readonly IApplicationService applicationService;
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="eventService"></param>
		/// <param name="userService"></param>
		/// <param name="applicationService"></param>
		public EventsController(IEventService eventService, IUserService userService, IApplicationService applicationService)
		{
			this.eventService = eventService;
			this.userService = userService;
			this.applicationService = applicationService;
		}

		#endregion
		
		#region Index

		[HttpGet]
		public IActionResult Index()
		{
			var viewModel = new EventsViewModel();
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
		[RbacObject("{id}")]
		public async Task<IActionResult> Detail(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return this.RedirectToAction("Index");
			}
			
			var token = this.GetBearerToken();
			var getEventResponse = await this.eventService.GetAsync(id, token);
			if (getEventResponse.IsSuccess)
			{
				var (user, application) = await this.GetUtilizerPair(getEventResponse.Data.UtilizerId, token);
				
				var viewModel = new EventDetailViewModel
				{
					Id = getEventResponse.Data.Id,
					Type = getEventResponse.Data.EventType,
					UtilizerId = getEventResponse.Data.UtilizerId,
					EventTime = getEventResponse.Data.EventTime,
					Document = getEventResponse.Data.Document,
					Prior = getEventResponse.Data.Prior,
					User = user,
					Application = application
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
				var viewModel = new EventDetailViewModel();
				viewModel.SetError(getEventResponse);

				return View(viewModel);
			}
		}

		private async Task<Tuple<User, Application>> GetUtilizerPair(string utilizerId, TokenBase token)
		{
			if (utilizerId == null)
			{
				return new Tuple<User, Application>(null, null);
			}
			
			var getUserTask = this.userService.GetAsync(utilizerId, token);
			var getApplicationTask = this.applicationService.GetAsync(utilizerId, token);

			var tasks = new Task[]
			{
				getUserTask,
				getApplicationTask
			};

			Task.WaitAll(tasks);

			User user = null;
			var getUserResponse = await getUserTask;
			if (getUserResponse.IsSuccess)
			{
				user = getUserResponse.Data;
			}

			Application application = null;
			var getApplicationResponse = await getApplicationTask;
			if (getApplicationResponse.IsSuccess)
			{
				application = getApplicationResponse.Data;
			}

			return new Tuple<User, Application>(user, application);
		}

		#endregion
	}
}