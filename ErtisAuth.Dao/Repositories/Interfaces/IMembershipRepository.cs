using Ertis.MongoDB.Repository;
using ErtisAuth.Dto.Models.Memberships;

namespace ErtisAuth.Dao.Repositories.Interfaces
{
	public interface IMembershipRepository : IMongoRepository<MembershipDto>
	{
		
	}
}