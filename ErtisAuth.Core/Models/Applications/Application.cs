using Ertis.Core.Models.Resources;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Applications
{
	public class Application : MembershipBoundedResource, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("role")]
		public string Role { get; set; }
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }

		#endregion
	}
}