using System.Threading.Tasks;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Extensions.Authorization.Annotations;
using ErtisAuth.Hub.Extensions;
using ErtisAuth.Hub.ViewModels;
using ErtisAuth.Hub.ViewModels.UserTypes;
using ErtisAuth.Identity.Attributes;
using ErtisAuth.Sdk.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ErtisAuth.Hub.Controllers
{
    [Authorized]
    [RbacResource("memberships")]
    [Route("memberships/{membershipId}")]
    public class UserTypesController : Controller
    {
        #region Services

        private readonly IMembershipService membershipService;

        #endregion

        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="membershipService"></param>
        public UserTypesController(IMembershipService membershipService)
        {
            this.membershipService = membershipService;
        }

        #endregion
        
        #region Detail

        [HttpGet("user-type")]
        [RbacObject("{membershipId}")]
        public async Task<IActionResult> Detail(string membershipId)
        {
            var token = this.GetBearerToken();
            var getMembershipResult = await this.membershipService.GetMembershipAsync(membershipId, token);
            if (getMembershipResult.IsSuccess)
            {
                var membership = getMembershipResult.Data;
                var viewModel = new UserTypeViewModel
                {
                    Membership = membership
                };
                
                
                
                return View(viewModel);   
            }
            else
            {
                var viewModel = new SerializableViewModel();
                viewModel.SetError(getMembershipResult);
                this.SetRedirectionParameter(viewModel);
                return this.RedirectToAction("Index", "Memberships");
            }
        }

        #endregion
        
        #region Update

		[HttpPost]
		[RbacAction(Rbac.CrudActions.Update)]
		public async Task<IActionResult> Update([FromForm] UserTypeViewModel model)
		{
            return this.RedirectToAction("Detail", routeValues: new { membershipId = model.Membership.Id });
		}

		#endregion
    }
}

/*
	UserType userType = null;
	if (model.UserType != null && !string.IsNullOrEmpty(model.UserTypeJson))
	{
		var userTypeDefinitionsObject = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<UserTypePayload>>(model.UserTypeJson);
		if (userTypeDefinitionsObject != null)
		{
			var userTypeDefinitions = userTypeDefinitionsObject.ToArray();
			var expandoObject = new ExpandoObject();
			var expandoObjectDictionary = (ICollection<KeyValuePair<string, object>>)expandoObject;
			var propertiesDictionary = userTypeDefinitions.ToDictionary<UserTypePayload, string, object>(
				userTypeDefinition => userTypeDefinition.Name, 
				userTypeDefinition => new
				{
					type = userTypeDefinition.Type, 
					title = userTypeDefinition.Title, 
					description = userTypeDefinition.Description
				});

			foreach (var pair in propertiesDictionary)
			{
				expandoObjectDictionary.Add(pair);
			}

			dynamic properties = expandoObject;
			
			userType = new UserType
			{
				Title = model.UserType.Title,
				Description = model.UserType.Description,
				Properties = properties,
				RequiredFields = userTypeDefinitions.Where(x => x.IsRequired).Select(x => x.Name).ToArray()
			};
		}
	}
	else if (model.UserType == null && !string.IsNullOrEmpty(model.NewUserTypeName))
	{
		userType = new UserType
		{
			Title = model.NewUserTypeName,
			Description = model.NewUserTypeDescription,
			Properties = new
			{
				type = new
				{
					type = "string",
					title = "User Type",
					description = "Custom User Type"
				}
			},
			RequiredFields = Array.Empty<string>()
		};
	}
	*/