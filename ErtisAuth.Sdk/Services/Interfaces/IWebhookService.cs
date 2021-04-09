using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Identity;
using ErtisAuth.Core.Models.Webhooks;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IWebhookService
	{
		IResponseResult<Webhook> CreateWebhook(Webhook webhook, TokenBase token);
		
		Task<IResponseResult<Webhook>> CreateWebhookAsync(Webhook webhook, TokenBase token);
		
		IResponseResult<Webhook> GetWebhook(string webhookId, TokenBase token);
		
		Task<IResponseResult<Webhook>> GetWebhookAsync(string webhookId, TokenBase token);
		
		IResponseResult<IPaginationCollection<Webhook>> GetWebhooks(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		Task<IResponseResult<IPaginationCollection<Webhook>>> GetWebhooksAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		IResponseResult<IPaginationCollection<Webhook>> QueryWebhooks(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		Task<IResponseResult<IPaginationCollection<Webhook>>> QueryWebhooksAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		IResponseResult<Webhook> UpdateWebhook(Webhook webhook, TokenBase token);
		
		Task<IResponseResult<Webhook>> UpdateWebhookAsync(Webhook webhook, TokenBase token);
		
		IResponseResult DeleteWebhook(string webhookId, TokenBase token);
		
		Task<IResponseResult> DeleteWebhookAsync(string webhookId, TokenBase token);
	}
}