using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.Applications;

namespace ErtisAuth.Abstractions.Services
{
	public interface IApplicationService : IMembershipBoundedCrudService<Application>
	{
		Application GetById(string id);
		
		ValueTask<Application> GetByIdAsync(string id, CancellationToken cancellationToken = default);

		bool IsSystemReservedApplication(Application application);
	}
}