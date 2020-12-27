using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.UserTypes;

namespace ErtisAuth.Dao.Repositories
{
	public class UserTypeRepository : MongoRepositoryBase<UserTypeDto>, IUserTypeRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public UserTypeRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "user_types", actionBinder)
		{
			
		}

		#endregion
	}
}