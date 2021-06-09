using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class EventService : MembershipBoundedService, IEventService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public EventService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
		
		#region Create Methods
		
		public IResponseResult<ErtisAuthEvent> CreateErtisAuthEvent(ErtisAuthEvent ertisAuthEvent, TokenBase token) =>
			this.CreateErtisAuthEventAsync(ertisAuthEvent, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<ErtisAuthEvent>> CreateErtisAuthEventAsync(ErtisAuthEvent ertisAuthEvent, TokenBase token)
		{
			return await this.ExecuteRequestAsync<ErtisAuthEvent>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/events", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(ertisAuthEvent));
		}
		
		#endregion
		
		#region Read Methods
		
		public IResponseResult<ErtisAuthEvent> GetErtisAuthEvent(string ertisAuthEventId, TokenBase token) =>
			this.GetErtisAuthEventAsync(ertisAuthEventId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<ErtisAuthEvent>> GetErtisAuthEventAsync(string ertisAuthEventId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<ErtisAuthEvent>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/events/{ertisAuthEventId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		public IResponseResult<IPaginationCollection<ErtisAuthEvent>> GetErtisAuthEvents(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null, 
			string searchKeyword = null) =>
			this.GetErtisAuthEventsAsync(
				token,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection,
				searchKeyword)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<ErtisAuthEvent>>> GetErtisAuthEventsAsync(
			TokenBase token,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null,
			string searchKeyword = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<ErtisAuthEvent>>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/events", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
		
		#region Query Methods
		
		public IResponseResult<IPaginationCollection<ErtisAuthEvent>> QueryErtisAuthEvents(
			TokenBase token, 
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.QueryErtisAuthEventsAsync(
				token,
				query,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<ErtisAuthEvent>>> QueryErtisAuthEventsAsync(
			TokenBase token,
			string query,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<ErtisAuthEvent>>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/events/_query", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
		}
		
		#endregion
		
		#region Update Methods
		
		public IResponseResult<ErtisAuthEvent> UpdateErtisAuthEvent(ErtisAuthEvent ertisAuthEvent, TokenBase token) =>
			this.UpdateErtisAuthEventAsync(ertisAuthEvent, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<ErtisAuthEvent>> UpdateErtisAuthEventAsync(ErtisAuthEvent ertisAuthEvent, TokenBase token)
		{
			if (string.IsNullOrEmpty(ertisAuthEvent.Id))
			{
				return new ResponseResult<ErtisAuthEvent>(false, "ErtisAuthEvent id is required!");
			}
			
			return await this.ExecuteRequestAsync<ErtisAuthEvent>(
				HttpMethod.Put, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/events/{ertisAuthEvent.Id}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(ertisAuthEvent));
		}
		
		#endregion
		
		#region Delete Methods
		
		public IResponseResult DeleteErtisAuthEvent(string ertisAuthEventId, TokenBase token) =>
			this.DeleteErtisAuthEventAsync(ertisAuthEventId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> DeleteErtisAuthEventAsync(string ertisAuthEventId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<ErtisAuthEvent>(
				HttpMethod.Delete, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/events/{ertisAuthEventId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
	}
}