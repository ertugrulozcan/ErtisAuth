using Ertis.Net.Rest;
using ErtisAuth.Sdk.Configuration;

namespace ErtisAuth.Sdk.Services
{
	public abstract class MembershipBoundedService : BaseRestService
	{
		#region Properties

		protected string AuthApiBaseUrl { get; }
		
		protected string AuthApiMembershipId { get; }
		
		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		protected MembershipBoundedService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(restHandler)
		{
			this.AuthApiBaseUrl = ertisAuthOptions.BaseUrl;
			this.AuthApiMembershipId = ertisAuthOptions.MembershipId;
		}

		#endregion
	}
}