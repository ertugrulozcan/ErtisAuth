using System;
using Ertis.Net.Rest;
using ErtisAuth.Extensions.Authorization.Constants;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace ErtisAuth.Extensions.AspNetCore.Configuration
{
	public class ErtisAuthBootloaderOptions
	{
		#region Properties

		public string Environment { get; set; }
		
		public string ConfigurationSectionName { get; set; }
		
		internal Type RestHandlerType { get; init; }
		
		public string AuthorizationSchemeName { get; set; }
		
		public string AuthorizationPolicyName { get; set; }
		
		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		internal ErtisAuthBootloaderOptions()
		{
			this.ConfigurationSectionName = "ErtisAuth";
			this.RestHandlerType = typeof(RestHandler);
			this.AuthorizationSchemeName = Constants.Schemes.ErtisAuthAuthorizationSchemeName;
			this.AuthorizationPolicyName = Policies.ErtisAuthAuthorizationPolicyName;
		}

		#endregion
	}
}