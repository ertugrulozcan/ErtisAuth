using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class ApplicationService : MembershipBoundedService, IApplicationService
	{
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
		
		#region Methods

		public IResponseResult<Application> GetApplication(string id, TokenBase token) => this.GetApplicationAsync(id, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Application>> GetApplicationAsync(string id, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Application>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/applications/{id}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}

		#endregion
	}
}