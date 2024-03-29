using Ertis.Core.Models.Resources;
using ErtisAuth.Integrations.OAuth.Core;
using Newtonsoft.Json;

namespace ErtisAuth.Core.Models.Providers
{
	public class Provider : MembershipBoundedResource, IHasSysInfo
	{
		#region Properties

		[JsonProperty("name")]
		public string Name { get; }
		
		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("defaultRole")]
		public string DefaultRole { get; set; }
		
		[JsonProperty("defaultUserType")]
		public string DefaultUserType { get; set; }
		
		[JsonProperty("appClientId")]
		public string AppClientId { get; set; }
		
		[JsonProperty("teamId")]
		public string TeamId { get; set; }
		
		[JsonProperty("tenantId")]
		public string TenantId { get; set; }
		
		[JsonProperty("privateKey")]
		public string PrivateKey { get; set; }
		
		[JsonProperty("privateKeyId")]
		public string PrivateKeyId { get; set; }
		
		[JsonProperty("redirectUri")]
		public string RedirectUri { get; set; }
		
		[JsonProperty("isActive")]
		public bool? IsActive { get; set; }
		
		[JsonProperty("sys")]
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