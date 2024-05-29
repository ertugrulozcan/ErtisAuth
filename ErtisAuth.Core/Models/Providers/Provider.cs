using System.Text.Json.Serialization;
using Ertis.Core.Models.Resources;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Providers
{
	public class Provider : MembershipBoundedResource, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		[JsonPropertyName("name")]
		public string Name { get; }
		
		[JsonProperty("description")]
		[JsonPropertyName("description")]
		public string Description { get; set; }

		[JsonProperty("defaultRole")]
		[JsonPropertyName("defaultRole")]
		public string DefaultRole { get; set; }
		
		[JsonProperty("defaultUserType")]
		[JsonPropertyName("defaultUserType")]
		public string DefaultUserType { get; set; }
		
		[JsonProperty("appClientId")]
		[JsonPropertyName("appClientId")]
		public string AppClientId { get; set; }
		
		[JsonProperty("teamId")]
		[JsonPropertyName("teamId")]
		public string TeamId { get; set; }
		
		[JsonProperty("tenantId")]
		[JsonPropertyName("tenantId")]
		public string TenantId { get; set; }
		
		[JsonProperty("privateKey")]
		[JsonPropertyName("privateKey")]
		public string PrivateKey { get; set; }
		
		[JsonProperty("privateKeyId")]
		[JsonPropertyName("privateKeyId")]
		public string PrivateKeyId { get; set; }
		
		[JsonProperty("redirectUri")]
		[JsonPropertyName("redirectUri")]
		public string RedirectUri { get; set; }
		
		[JsonProperty("isActive")]
		[JsonPropertyName("isActive")]
		public bool? IsActive { get; set; }
		
		[JsonProperty("sys")]
		[JsonPropertyName("sys")]
		public SysModel Sys { get; set; }
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="provider"></param>
		public Provider(KnownProviders provider)
		{
			this.Name = provider.ToString();
		}

		#endregion
	}
}