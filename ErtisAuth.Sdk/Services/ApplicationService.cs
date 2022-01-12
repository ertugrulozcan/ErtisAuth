using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class ApplicationService : MembershipBoundedService<Application>, IApplicationService
	{
		#region Properties

		protected override string Slug => "applications";	

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public ApplicationService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
	}
}