using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Memberships
{
	public class Membership : ResourceBase
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }

		#endregion
	}
}