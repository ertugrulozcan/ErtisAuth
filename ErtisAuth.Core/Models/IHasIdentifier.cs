using Newtonsoft.Json;

namespace ErtisAuth.Core.Models
{
	public interface IHasIdentifier
	{
		#region Properties

		[JsonProperty("_id")]
		string Id { get; set; }

		#endregion
	}
}