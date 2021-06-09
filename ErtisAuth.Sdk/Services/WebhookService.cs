using System.Net.Http;
using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using Ertis.Net.Http;
using Ertis.Net.Rest;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Webhooks;
using ErtisAuth.Sdk.Configuration;
using ErtisAuth.Sdk.Helpers;
using ErtisAuth.Sdk.Services.Interfaces;

namespace ErtisAuth.Sdk.Services
{
	public class WebhookService : MembershipBoundedService, IWebhookService
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="ertisAuthOptions"></param>
		/// <param name="restHandler"></param>
		public WebhookService(IErtisAuthOptions ertisAuthOptions, IRestHandler restHandler) : base(ertisAuthOptions, restHandler)
		{
			
		}

		#endregion
		
		#region Create Methods
		
		public IResponseResult<Webhook> CreateWebhook(Webhook webhook, TokenBase token) =>
			this.CreateWebhookAsync(webhook, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Webhook>> CreateWebhookAsync(Webhook webhook, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Webhook>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/webhooks", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(webhook));
		}
		
		#endregion
		
		#region Read Methods
		
		public IResponseResult<Webhook> GetWebhook(string webhookId, TokenBase token) =>
			this.GetWebhookAsync(webhookId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Webhook>> GetWebhookAsync(string webhookId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Webhook>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/webhooks/{webhookId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		public IResponseResult<IPaginationCollection<Webhook>> GetWebhooks(
			TokenBase token, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null, 
			string searchKeyword = null) =>
			this.GetWebhooksAsync(
				token,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection,
				searchKeyword)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Webhook>>> GetWebhooksAsync(
			TokenBase token,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null,
			string searchKeyword = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Webhook>>(
				HttpMethod.Get, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/webhooks", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
		
		#region Query Methods
		
		public IResponseResult<IPaginationCollection<Webhook>> QueryWebhooks(
			TokenBase token, 
			string query, 
			int? skip = null, 
			int? limit = null, 
			bool? withCount = null, 
			string orderBy = null, 
			SortDirection? sortDirection = null) =>
			this.QueryWebhooksAsync(
				token,
				query,
				skip,
				limit,
				withCount,
				orderBy,
				sortDirection)
				.ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<IPaginationCollection<Webhook>>> QueryWebhooksAsync(
			TokenBase token,
			string query,
			int? skip = null,
			int? limit = null,
			bool? withCount = null,
			string orderBy = null,
			SortDirection? sortDirection = null)
		{
			return await this.ExecuteRequestAsync<PaginationCollection<Webhook>>(
				HttpMethod.Post, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/webhooks/_query", 
				QueryStringHelper.GetQueryString(skip, limit, withCount, orderBy, sortDirection), 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(Newtonsoft.Json.JsonConvert.DeserializeObject(query)));
		}
		
		#endregion
		
		#region Update Methods
		
		public IResponseResult<Webhook> UpdateWebhook(Webhook webhook, TokenBase token) =>
			this.UpdateWebhookAsync(webhook, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult<Webhook>> UpdateWebhookAsync(Webhook webhook, TokenBase token)
		{
			if (string.IsNullOrEmpty(webhook.Id))
			{
				return new ResponseResult<Webhook>(false, "Webhook id is required!");
			}
			
			return await this.ExecuteRequestAsync<Webhook>(
				HttpMethod.Put, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/webhooks/{webhook.Id}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()),
				new JsonRequestBody(webhook));
		}
		
		#endregion
		
		#region Delete Methods
		
		public IResponseResult DeleteWebhook(string webhookId, TokenBase token) =>
			this.DeleteWebhookAsync(webhookId, token).ConfigureAwait(false).GetAwaiter().GetResult();

		public async Task<IResponseResult> DeleteWebhookAsync(string webhookId, TokenBase token)
		{
			return await this.ExecuteRequestAsync<Webhook>(
				HttpMethod.Delete, 
				$"{this.AuthApiBaseUrl}/memberships/{this.AuthApiMembershipId}/webhooks/{webhookId}", 
				null, 
				HeaderCollection.Add("Authorization", token.ToString()));
		}
		
		#endregion
	}
}