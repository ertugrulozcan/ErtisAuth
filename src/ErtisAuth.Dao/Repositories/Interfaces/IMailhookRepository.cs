using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Mailing;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IMailhookRepository : IMongoRepository<Mailhook>;