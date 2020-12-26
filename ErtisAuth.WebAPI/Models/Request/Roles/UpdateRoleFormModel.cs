using System.Collections.Generic;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Roles
{
	public class UpdateRoleFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }
		
		[JsonProperty("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		#endregion
	}
}