using Ertis.Data.Repository;
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
		/// <param name="settings"></param>
		/// <param name="clientSettings"></param>
		/// <param name="actionBinder"></param>
		public RoleRepository(IDatabaseSettings settings, IClientSettings clientSettings, IRepositoryActionBinder actionBinder) : base(settings, "roles", clientSettings, actionBinder)
		{
			
		}

		#endregion
	}
}