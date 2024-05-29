using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models
{
	public interface IHasMembership
	{
		[JsonProperty("membership_id")]
		[JsonPropertyName("membership_id")]
		string MembershipId { get; set; }
	}
}