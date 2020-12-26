using System.Threading.Tasks;
using ErtisAuth.Core.Models.Events;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IEventService
	{
		Task FireEventAsync(ErtisAuthEvent ertisAuthEvent);
	}
}