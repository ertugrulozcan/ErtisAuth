using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Hub.Constants;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.Tests;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Hub.Controllers
{
    [Route("tests")]
    public class TestController : Controller
    {
        #region Services

        private readonly IRoleService roleService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="roleService"></param>
        public TestController(IRoleService roleService)
        {
            this.roleService = roleService;
        }

        #endregion
        
        #region Index

        [HttpGet("check-permission-tests")]
        public async Task<IActionResult> CheckPermissionTests()
        {
            var resources = new[]
            {
                "memberships",
                "users",
                "applications",
                "roles",
                "events",
                "providers",
                "tokens",
                "webhooks",
                "mailhooks"
            };

            var crudActions = new[]
            {
                Rbac.CrudActions.Read,
                Rbac.CrudActions.Create,
                Rbac.CrudActions.Update,
                Rbac.CrudActions.Delete
            };

            CheckPermissionTestsViewModel viewModel;
            
            try
            {
                var userId = this.GetClaim(Claims.UserId);
                var subjectSegment = new RbacSegment(userId);

                var token = this.GetBearerToken();

                var stopwatch = Stopwatch.StartNew();
                var tasks = new List<Task<CheckPermissionTestResult>>();
                foreach (var resource in resources)
                {
                    foreach (var crudAction in crudActions)
                    {
                        var rbac = new Rbac(
                            subjectSegment, 
                            new RbacSegment(resource), 
                            Rbac.GetSegment(crudAction),
                            RbacSegment.All);

                        tasks.Add(GetCheckPermissionTestResultAsync(rbac, token));
                    }
                }

                viewModel = new CheckPermissionTestsViewModel
                {
                    Results = await Task.WhenAll(tasks)
                };
            
                stopwatch.Stop();
                viewModel.TotalTime = stopwatch.Elapsed;
            }
            catch (Exception ex)
            {
                viewModel = new CheckPermissionTestsViewModel
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }

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

        private async Task<CheckPermissionTestResult> GetCheckPermissionTestResultAsync(Rbac rbac, TokenBase token)
        {
            return new CheckPermissionTestResult
            {
                Rbac = rbac,
                IsPermitted = await this.roleService.CheckPermissionAsync(rbac.ToString(), token)
            };
        }

        #endregion
    }
}