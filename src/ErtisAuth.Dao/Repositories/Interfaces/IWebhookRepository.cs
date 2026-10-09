using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Webhooks;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IWebhookRepository : IMongoRepository<Webhook>;