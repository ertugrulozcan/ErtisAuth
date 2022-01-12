using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Providers;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class ProviderService : ReadonlyMembershipBoundedService<OAuthProvider>, IProviderService
	{
		#region Properties

		protected override string Slug => "providers";	

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public ProviderService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
	}
}