using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Dao.Repositories
{
	public class ActiveTokensRepository : MongoRepositoryBase<ActiveTokenDto>, IActiveTokensRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		public ActiveTokensRepository(IDatabaseSettings settings) : base(settings, "active_tokens")
		{
			
		}

		#endregion
	}
}