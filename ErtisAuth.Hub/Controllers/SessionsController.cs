using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Sessions;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
    [RbacResource("sessions")]
    [Route("sessions")]
    public class SessionsController : Controller
    {
        #region Services

        private readonly IActiveTokensService activeTokensService;

        #endregion
        
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="activeTokensService"></param>
        public SessionsController(IActiveTokensService activeTokensService)
        {
            this.activeTokensService = activeTokensService;
        }

        #endregion
        
        #region Index

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var token = this.GetBearerToken();
            var activeTokensResult = await this.activeTokensService.GetAsync(token);
            var activeTokens = activeTokensResult.IsSuccess ? activeTokensResult.Data.Items.ToArray() : null;

            var groupedActiveTokensByCity = new Dictionary<string, List<object>>();
            if (activeTokens != null)
            {
                foreach (var activeToken in activeTokens)
                {
                    if (activeToken.ClientInfo is { GeoLocation: { Location: { }}} && !string.IsNullOrEmpty(activeToken.ClientInfo.GeoLocation.City))
                    {
                        var city = activeToken.ClientInfo.GeoLocation.City;
                        if (!groupedActiveTokensByCity.ContainsKey(city))
                        {
                            groupedActiveTokensByCity.Add(city, new List<object>());
                        }
                        
                        groupedActiveTokensByCity[city].Add(new
                        {
                            client_info = activeToken.ClientInfo,
                        });
                    }
                }   
            }
            
            var groupedActiveTokensByCountry = new Dictionary<string, List<object>>();
            if (activeTokens != null)
            {
                foreach (var activeToken in activeTokens)
                {
                    if (activeToken.ClientInfo is { GeoLocation: { Location: { }}} && !string.IsNullOrEmpty(activeToken.ClientInfo.GeoLocation.Country))
                    {
                        var country = activeToken.ClientInfo.GeoLocation.Country;
                        if (!groupedActiveTokensByCountry.ContainsKey(country))
                        {
                            groupedActiveTokensByCountry.Add(country, new List<object>());
                        }
                        
                        groupedActiveTokensByCountry[country].Add(new
                        {
                            client_info = activeToken.ClientInfo,
                        });
                    }
                }   
            }

            var viewModel = new SessionsViewModel
            {
                GroupedActiveTokensByCity = groupedActiveTokensByCity,
                GroupedActiveTokensByCountry = groupedActiveTokensByCountry
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
    }
}