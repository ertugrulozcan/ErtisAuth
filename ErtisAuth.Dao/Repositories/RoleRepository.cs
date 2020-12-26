using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Roles;

namespace ErtisAuth.Dao.Repositories
{
	public class RoleRepository : MongoRepositoryBase<RoleDto>, IRoleRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public RoleRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "roles", actionBinder)
		{
			
		}

		#endregion
	}
}