using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Identity
{
	public interface IUtilizer
	{
		#region Properties

		[JsonProperty("_id")]
		public string Id { get; set; }
		
		[JsonProperty("role")]
		public string Role { get; set; }
		
		[JsonIgnore]
		public Utilizer.UtilizerType UtilizerType { get; }

		#endregion
	}
}