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
		
		#region Fire Custom Event Methods

		public IResponseResult<ErtisAuthCustomEvent> FireCustomEvent(string eventType, string utilizerId, object document, object prior, TokenBase token) =>
			this.FireCustomEventAsync(eventType, utilizerId, document, prior, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<ErtisAuthCustomEvent>> FireCustomEventAsync(string eventType, string utilizerId, object document, object prior, TokenBase token)
		{
			var ertisAuthCustomEvent = new ErtisAuthCustomEvent
			{
				EventType = eventType,
				UtilizerId = utilizerId,
				Document = document,
				Prior = prior,
				MembershipId = this.AuthApiMembershipId
			};
			
			return await this.ExecuteRequestAsync<ErtisAuthCustomEvent>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/events", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(ertisAuthCustomEvent));	
		}
		
		#endregion
	}
}