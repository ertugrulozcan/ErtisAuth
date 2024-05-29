using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models
{
	public abstract class MembershipBoundedResource : ResourceBase, IHasMembership
	{
		#region Properties

		[JsonProperty("membership_id")]
		[JsonPropertyName("membership_id")]
		public string MembershipId { get; set; }

		#endregion
	}
}