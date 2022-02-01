using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ErtisAuth.Core.Models.Roles;
using ErtisAuth.Core.Models.Users;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;

namespace ErtisAuth.Hub.ViewModels.Users
{
    public abstract class UserViewModelBase : ViewModelBase, IHasUserTypeUserViewModel
    {
        #region Properties
        
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; }
		
        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; }
		
        [Required(ErrorMessage = "Username is required.")]
        [MinLength(3, ErrorMessage = "The username field's minimum length must be of 3 characters")]
        [RegularExpression("^[A-Za-z].(\\S*)", ErrorMessage = "The username can not starts with a digit and can not contains space and its minimum length must be of 3 characters.")]
        public string Username { get; set; }
		
        [Required(ErrorMessage = "Email Address is required.")]
        [EmailAddress(ErrorMessage = "The value is not a valid email address")]
        public string EmailAddress { get; set; }
		
        public string RoleId { get; set; }
        
        public string Role { get; set; }

        public string MembershipId { get; set; }

        public List<SelectListItem> RoleList { get; set; }

        public UserType UserType { get; set; }
		
        public dynamic AdditionalProperties { get; set; }
        
        public string UbacJson { get; set; }
        
        #endregion
        
        #region Methods
        public IEnumerable<Ubac> ParsePermissionArray()
        {
	        return this.ParseRbacArray("permissions");
        }
		
        public IEnumerable<Ubac> ParseForbiddenArray()
        {
	        return this.ParseRbacArray("forbidden");
        }
		
        private IEnumerable<Ubac> ParseRbacArray(string rootField)
        {
	        if (!string.IsNullOrEmpty(this.UbacJson))
	        {
		        var jsonPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(this.UbacJson);
		        if (jsonPayload is JObject jObject && jObject.ContainsKey(rootField))
		        {
			        if (jObject[rootField] is JArray jArray)
			        {
				        foreach (var jToken in jArray)
				        {
					        var resourceStr = jToken["Resource"]?.ToString().Trim();
					        var resource = resourceStr == "*" ? RbacSegment.All : new RbacSegment(resourceStr);
							
					        var actionStr = jToken["Action"]?.ToString().Trim();
					        var action = actionStr == "*" ? RbacSegment.All : new RbacSegment(actionStr);
							
					        var objectStr = jToken["Object"]?.ToString().Trim();
					        var obj = objectStr == "*" ? RbacSegment.All : new RbacSegment(objectStr);
					        yield return new Ubac(resource, action, obj);
				        }
			        }
		        }
	        }
        }
        #endregion
    }
}