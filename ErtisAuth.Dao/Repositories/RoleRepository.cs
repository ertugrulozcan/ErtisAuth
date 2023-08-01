using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Roles;

namespace ErtisAuth.Dao.Repositories
{
	public class RoleRepository : RepositoryBase<RoleDto>, IRoleRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public RoleRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
			base(clientProvider, settings, "roles", actionBinder)
		{
			
		}

		#endregion
	}
}