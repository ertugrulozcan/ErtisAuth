using Newtonsoft.Json;

namespace ErtisAuth.Core.Models
{
	public abstract class ResourceBase
	{
		#region Properties

		[JsonProperty("_id")]
		public string Id { get; set; }

		#endregion
	}
}