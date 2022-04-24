using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Users;

namespace ErtisAuth.Dao.Repositories
{
	public class UserRepository_OLD : MongoRepositoryBase<UserDto>, IUserRepository_OLD
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public UserRepository_OLD(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "users", actionBinder)
		{
			
		}

		#endregion
	}
}