using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public interface IUtilizer
	{
		#region Properties

		[JsonProperty("_id")]
		string Id { get; set; }
		
		[JsonProperty("role")]
		string Role { get; set; }
		
		[JsonIgnore]
		Utilizer.UtilizerType UtilizerType { get; }
		
		[JsonProperty("membership_id")]
		string MembershipId { get; set; }

		#endregion
	}
}