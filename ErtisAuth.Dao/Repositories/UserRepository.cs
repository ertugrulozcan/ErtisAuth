using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;

namespace ErtisAuth.Dao.Repositories
{
	public class UserRepository : MongoRepositoryBase<UserDto>, IUserRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		public UserRepository(IDatabaseSettings settings) : base(settings, "users")
		{
			
		}

		#endregion
	}
}