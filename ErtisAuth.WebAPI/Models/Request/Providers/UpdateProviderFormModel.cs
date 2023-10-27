using Newtonsoft.Json;

namespace ErtisAuth.WebAPI.Models.Request.Providers
{
	public class UpdateProviderFormModel
	{
		#region Properties

		[JsonProperty("_id")]
		public string Id { get; set; }
		
		[JsonProperty("name")]
		public string Name { get; set; }
		
		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("defaultRole")]
		public string DefaultRole { get; set; }
		
		[JsonProperty("defaultUserType")]
		public string DefaultUserType { get; set; }
		
		[JsonProperty("appClientId")]
		public string AppClientId { get; set; }
		
		[JsonProperty("tenantId")]
		public string TenantId { get; set; }
		
		[JsonProperty("teamId")]
		public string TeamId { get; set; }
		
		[JsonProperty("privateKey")]
		public string PrivateKey { get; set; }
		
		[JsonProperty("privateKeyId")]
		public string PrivateKeyId { get; set; }
		
		[JsonProperty("redirectUri")]
		public string RedirectUri { get; set; }
		
		[JsonProperty("isActive")]
		public bool? IsActive { get; set; }
		
		[JsonProperty("membership_id")]
		public string MembershipId { get; set; }

		#endregion
	}
}