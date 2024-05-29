using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models
{
	public interface IHasIdentifier
	{
		#region Properties

		[JsonProperty("_id")]
		[JsonPropertyName("_id")]
		string Id { get; set; }

		#endregion
	}
}