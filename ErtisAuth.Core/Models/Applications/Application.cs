using Ertis.Core.Models.Resources;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Applications
{
	public class Application : MembershipBoundedResource, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("slug")]
		public string Slug { get; set; }
		
		[JsonProperty("secret")]
		public string Secret { get; set; }
		
		[JsonProperty("role")]
		public string Role { get; set; }
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }

		#endregion
	}
}