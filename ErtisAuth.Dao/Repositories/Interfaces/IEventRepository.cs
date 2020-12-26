using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Events;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface IEventRepository : IMongoRepository<EventDto>
	{
		
	}
}