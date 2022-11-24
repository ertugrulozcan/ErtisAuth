using ErtisAuth.Integrations.OAuth;
using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Providers
{
	public class CreateProviderFormModel
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }
		
		[JsonProperty("defaultRole")]
		public string DefaultRole { get; set; }
		
		[JsonProperty("appClientId")]
		public string AppClientId { get; set; }
		
		[JsonProperty("tenantId")]
		public string TenantId { get; set; }
		
		[JsonProperty("isActive")]
		public bool? IsActive { get; set; }
		
		#endregion
	}
}