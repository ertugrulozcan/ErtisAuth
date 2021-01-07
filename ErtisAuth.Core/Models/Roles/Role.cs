using System.Collections.Generic;
using Ertis.Core.Models.Resources;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Roles
{
	public class Role : MembershipBoundedResource, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("permissions")]
		public IEnumerable<string> Permissions { get; set; }
		
		[JsonProperty("forbidden")]
		public IEnumerable<string> Forbidden { get; set; }
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}