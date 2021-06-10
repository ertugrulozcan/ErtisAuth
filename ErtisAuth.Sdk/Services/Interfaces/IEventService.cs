using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Events;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IEventService
	{
		IResponseResult<ErtisAuthEvent> CreateErtisAuthEvent(ErtisAuthEvent ertisAuthEvent, TokenBase token);
		
		Task<IResponseResult<ErtisAuthEvent>> CreateErtisAuthEventAsync(ErtisAuthEvent ertisAuthEvent, TokenBase token);
		
		IResponseResult<ErtisAuthEvent> GetErtisAuthEvent(string ertisAuthEventId, TokenBase token);
		
		Task<IResponseResult<ErtisAuthEvent>> GetErtisAuthEventAsync(string ertisAuthEventId, TokenBase token);
		
		IResponseResult<IPaginationCollection<ErtisAuthEvent>> GetErtisAuthEvents(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		Task<IResponseResult<IPaginationCollection<ErtisAuthEvent>>> GetErtisAuthEventsAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		IResponseResult<IPaginationCollection<ErtisAuthEvent>> QueryErtisAuthEvents(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		Task<IResponseResult<IPaginationCollection<ErtisAuthEvent>>> QueryErtisAuthEventsAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		IResponseResult<ErtisAuthCustomEvent> FireCustomEvent(string eventType, string utilizerId, object document, object prior, TokenBase token);
		
		Task<IResponseResult<ErtisAuthCustomEvent>> FireCustomEventAsync(string eventType, string utilizerId, object document, object prior, TokenBase token);
	}
}