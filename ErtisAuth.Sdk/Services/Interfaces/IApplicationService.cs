using System.Threading.Tasks;
using Ertis.Core.Models.Response;
using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	public interface IApplicationService
	{
		IResponseResult<Application> GetApplication(string id, TokenBase token);
		
		Task<IResponseResult<Application>> GetApplicationAsync(string id, TokenBase token);
	}
}