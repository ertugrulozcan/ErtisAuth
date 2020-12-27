using Ertis.Core.Models.Resources;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.UserTypes
{
	public class UserType : MembershipBoundedResource, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }
		
		[JsonProperty("slug")]
		public string Slug { get; set; }
		
		[JsonProperty("schema")]
		public SchemaProperty Schema { get; set; }
		
		[JsonProperty("sys")]
		public SysModel Sys { get; set; }
		
		#endregion
	}
}