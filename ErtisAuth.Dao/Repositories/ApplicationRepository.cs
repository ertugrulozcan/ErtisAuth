using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Applications;

namespace ErtisAuth.Dao.Repositories
{
	public class ApplicationRepository : MongoRepositoryBase<ApplicationDto>, IApplicationRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public ApplicationRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "applications", actionBinder)
		{
			
		}

		#endregion
	}
}