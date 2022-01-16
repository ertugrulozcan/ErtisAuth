using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ErtisAuth.Core.Models.Roles;
using Newtonsoft.Json.Linq;

namespace ErtisAuth.Hub.ViewModels.Roles
{
    public abstract class RoleViewModelBase : ViewModelBase
    {
        #region Properties

        [Required]
        public string Name { get; set; }
		
        public string Description { get; set; }
		
        public Rbac[] Permissions { get; set; }
		
        public Rbac[] Forbidden { get; set; }
		
        public Dictionary<RbacSegment, Rbac[]> PermissionsForBasicTab { get; set; }
        
        public string Json { get; set; }

        #endregion
		
        #region Methods

        public IEnumerable<Rbac> ParsePermissionArray()
        {
            return this.ParseRbacArray("permissions");
        }
		
        public IEnumerable<Rbac> ParseForbiddenArray()
        {
            return this.ParseRbacArray("forbidden");
        }
		
        private IEnumerable<Rbac> ParseRbacArray(string rootField)
        {
            if (!string.IsNullOrEmpty(this.Json))
            {
                var jsonPayload = Newtonsoft.Json.JsonConvert.DeserializeObject(this.Json);
                if (jsonPayload is JObject jObject && jObject.ContainsKey(rootField))
                {
                    if (jObject[rootField] is JArray jArray)
                    {
                        foreach (var jToken in jArray)
                        {
                            var subjectStr = jToken["Subject"]?.ToString().Trim();
                            var subject = subjectStr == "*" ? RbacSegment.All : new RbacSegment(subjectStr);
							
                            var resourceStr = jToken["Resource"]?.ToString().Trim();
                            var resource = resourceStr == "*" ? RbacSegment.All : new RbacSegment(resourceStr);
							
                            var actionStr = jToken["Action"]?.ToString().Trim();
                            var action = actionStr == "*" ? RbacSegment.All : new RbacSegment(actionStr);
							
                            var objectStr = jToken["Object"]?.ToString().Trim();
                            var obj = objectStr == "*" ? RbacSegment.All : new RbacSegment(objectStr);

                            yield return new Rbac(subject, resource, action, obj);
                        }
                    }
                }
            }
        }

        #endregion
    }
}