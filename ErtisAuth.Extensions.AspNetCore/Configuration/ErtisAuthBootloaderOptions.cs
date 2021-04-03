using System;
using Ertis.Net.Rest;
using ErtisAuth.Extensions.Authorization.Constants;

namespace ErtisAuth.Extensions.AspNetCore.Configuration
{
	public class ErtisAuthBootloaderOptions
	{
		#region Properties

		public string Environment { get; set; }
		
		public bool SetConfiguration { get; set; }

		public string ConfigurationSectionName { get; set; }
		
		public bool RegisterRestHandler { get; set; }
		
		internal Type RestHandlerType { get; private set; }
		
		public bool InitializeServices { get; set; }
		
		public string AuthorizationSchemeName { get; set; }
		
		public string AuthorizationPolicyName { get; set; }
		
		public bool SetDefaultAuthenticationHandler { get; set; }
		
		public bool SetDefaultAuthorizationHandler { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		internal ErtisAuthBootloaderOptions()
		{
			this.SetConfiguration = true;
			this.ConfigurationSectionName = "ErtisAuth";
			this.RegisterRestHandler = true;
			this.RestHandlerType = typeof(SystemRestHandler);
			this.InitializeServices = true;
			this.AuthorizationSchemeName = Constants.Schemes.ErtisAuthAuthorizationSchemeName;
			this.AuthorizationPolicyName = Policies.ErtisAuthAuthorizationPolicyName;
			this.SetDefaultAuthenticationHandler = true;
			this.SetDefaultAuthorizationHandler = true;
		}

		#endregion

		#region Methods

		public void SetRestHandler<T>() where T : IRestHandler
		{
			this.RestHandlerType = typeof(T);
		}

		#endregion
	}
}