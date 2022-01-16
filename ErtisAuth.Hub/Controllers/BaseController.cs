using System.Threading.Tasks;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Hub.Controllers
{
    public abstract class BaseController : Controller
    {
        #region Services

        private readonly IAuthenticationService authenticationService;
        private readonly IDeletableResourceService service;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="authenticationService"></param>
        /// <param name="service"></param>
        protected BaseController(IAuthenticationService authenticationService, IDeletableResourceService service)
        {
            this.authenticationService = authenticationService;
            this.service = service;
        }

        #endregion
        
        #region Delete

        [HttpPost("delete")]
        [RbacAction(Rbac.CrudActions.Delete)]
        public async Task<IActionResult> Delete([FromForm]DeleteViewModel deleteViewModel)
        {
            if (this.ModelState.IsValid)
            {
                var username = this.GetClaim(Claims.Username);
                var getTokenResponse = await this.authenticationService.GetTokenAsync(username, deleteViewModel.Password);
                if (getTokenResponse.IsSuccess)
                {
                    var deleteResponse = await this.service.DeleteAsync(deleteViewModel.ItemId, this.GetBearerToken());
                    if (deleteResponse.IsSuccess)
                    {
                        this.SetRedirectionParameter(new SerializableViewModel
                        {
                            IsSuccess = true,
                            SuccessMessage = "Successfully deleted"
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
        
        [HttpPost("bulk-delete")]
        [RbacAction(Rbac.CrudActions.Delete)]
        public async Task<IActionResult> BulkDelete([FromForm]BulkDeleteViewModel deleteViewModel)
        {
            if (this.ModelState.IsValid)
            {
                var username = this.GetClaim(Claims.Username);
                var getTokenResponse = await this.authenticationService.GetTokenAsync(username, deleteViewModel.Password);
                if (getTokenResponse.IsSuccess)
                {
                    var ids = Newtonsoft.Json.JsonConvert.DeserializeObject<string[]>(deleteViewModel.ItemIdsJson);
                    var deleteResponse = await this.service.BulkDeleteAsync(ids, this.GetBearerToken());
                    if (deleteResponse.IsSuccess)
                    {
                        this.SetRedirectionParameter(new SerializableViewModel
                        {
                            IsSuccess = true,
                            SuccessMessage = "Selected items deleted"
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