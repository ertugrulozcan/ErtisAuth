using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Memberships;

namespace ErtisAuth.Dao.Repositories
{
	public class MembershipRepository : RepositoryBase<MembershipDto>, IMembershipRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public MembershipRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
			base(clientProvider, settings, "memberships", actionBinder)
		{
			
		}

		#endregion
	}
}