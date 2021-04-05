using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
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
		
		#region Create Methods
		
		public IResponseResult<Application> CreateApplication(Application application, TokenBase token) =>
			this.CreateApplicationAsync(application, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Application>> CreateApplicationAsync(Application application, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Application>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/applications", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(application));
		}
		
		#endregion
		
		#region Read Methods
		
		public IResponseResult<Application> GetApplication(string applicationId, TokenBase token) =>
			this.GetApplicationAsync(applicationId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Application>> GetApplicationAsync(string applicationId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Application>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/application/{applicationId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		public IResponseResult<IPaginationCollection<Application>> GetApplications(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null, 
			string searchKeyword = null) =>
			this.GetApplicationsAsync(
				token,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection,
				searchKeyword)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Application>>> GetApplicationsAsync(
			TokenBase token,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null,
			string searchKeyword = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Application>>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/applications", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
		
		#region Query Methods
		
		public IResponseResult<IPaginationCollection<Application>> QueryApplications(
			TokenBase token, 
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.QueryApplicationsAsync(
				token,
				query,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Application>>> QueryApplicationsAsync(
			TokenBase token,
			string query,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Application>>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/applications", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
		}
		
		#endregion
		
		#region Update Methods
		
		public IResponseResult<Application> UpdateApplication(Application application, TokenBase token) =>
			this.UpdateApplicationAsync(application, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Application>> UpdateApplicationAsync(Application application, TokenBase token)
		{
			if (string.IsNullOrEmpty(application.Id))
			{
				return new ResponseResult<Application>(false, "Application id is required!");
			}
			
			return await this.ExecuteRequestAsync<Application>(
				HttpMethod.Put, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/applications/{application.Id}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(application));
		}
		
		#endregion
		
		#region Delete Methods
		
		public IResponseResult DeleteApplication(string applicationId, TokenBase token) =>
			this.DeleteApplicationAsync(applicationId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> DeleteApplicationAsync(string applicationId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Application>(
				HttpMethod.Delete, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/applications/{applicationId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
	}
}