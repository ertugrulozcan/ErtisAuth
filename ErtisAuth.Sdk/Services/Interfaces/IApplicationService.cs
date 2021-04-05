using System.Threading.Tasks;
using Ertis.Core.Collections;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IApplicationService
	{
		IResponseResult<Application> CreateApplication(Application application, TokenBase token);
		
		Task<IResponseResult<Application>> CreateApplicationAsync(Application application, TokenBase token);
		
		IResponseResult<Application> GetApplication(string applicationId, TokenBase token);
		
		Task<IResponseResult<Application>> GetApplicationAsync(string applicationId, TokenBase token);
		
		IResponseResult<IPaginationCollection<Application>> GetApplications(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		Task<IResponseResult<IPaginationCollection<Application>>> GetApplicationsAsync(TokenBase token, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null, string searchKeyword = null);
		
		IResponseResult<IPaginationCollection<Application>> QueryApplications(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		Task<IResponseResult<IPaginationCollection<Application>>> QueryApplicationsAsync(TokenBase token, string query, int? skip = null, int? limit = null, bool? withCount = null, string orderBy = null, SortDirection? sortDirection = null);
		
		IResponseResult<Application> UpdateApplication(Application application, TokenBase token);
		
		Task<IResponseResult<Application>> UpdateApplicationAsync(Application application, TokenBase token);
		
		IResponseResult DeleteApplication(string applicationId, TokenBase token);
		
		Task<IResponseResult> DeleteApplicationAsync(string applicationId, TokenBase token);
	}
}