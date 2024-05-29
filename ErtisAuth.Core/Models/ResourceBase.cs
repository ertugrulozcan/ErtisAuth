using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models
{
	public abstract class ResourceBase : IHasIdentifier
	{
		#region Properties

		[JsonProperty("_id")]
		[JsonPropertyName("_id")]
		public string Id { get; set; }

		#endregion
	}
}