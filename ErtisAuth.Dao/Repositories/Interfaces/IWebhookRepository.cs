using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Webhooks;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface IWebhookRepository : IMongoRepository<WebhookDto>
	{
		
	}
}