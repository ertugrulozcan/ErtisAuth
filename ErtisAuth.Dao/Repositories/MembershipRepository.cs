using Ertis.Data.Repository;
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
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public MembershipRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "memberships", actionBinder)
		{
			
		}

		#endregion
	}
}