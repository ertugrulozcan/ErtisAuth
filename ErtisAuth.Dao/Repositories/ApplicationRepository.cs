using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Applications;

namespace ErtisAuth.Dao.Repositories
{
	public class ApplicationRepository : RepositoryBase<ApplicationDto>, IApplicationRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public ApplicationRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
			base(clientProvider, settings, "applications", actionBinder)
		{
			
		}

		#endregion
	}
}