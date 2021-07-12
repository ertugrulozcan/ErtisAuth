using Ertis.Core.Models.Resources;
using ErtisAuth.Core.Models.Identity;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Applications
{
	public class Application : MembershipBoundedResource, IUtilizer, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("role")]
		public string Role { get; set; }
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		[JsonIgnore] 
		public Utilizer.UtilizerType UtilizerType => Utilizer.UtilizerType.Application;

		#endregion
	}
}