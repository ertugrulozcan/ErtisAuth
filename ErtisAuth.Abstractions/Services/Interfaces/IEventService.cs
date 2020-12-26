using System.Threading.Tasks;
using ErtisAuth.Core.Models.Events;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IEventService : IMembershipBoundedService<ErtisAuthEvent>
	{
		Task FireEventAsync(ErtisAuthEvent ertisAuthEvent);
	}
}