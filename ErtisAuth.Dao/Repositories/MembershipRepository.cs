using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Memberships;

namespace ErtisAuth.Dao.Repositories
{
	public class MembershipRepository : MongoRepositoryBase<MembershipDto>, IMembershipRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public MembershipRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "memberships", actionBinder)
		{
			
		}

		#endregion
	}
}