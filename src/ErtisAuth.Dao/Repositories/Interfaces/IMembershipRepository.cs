using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Memberships;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IMembershipRepository : IMongoRepository<Membership>;