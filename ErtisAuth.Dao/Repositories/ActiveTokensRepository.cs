using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Dao.Repositories
{
	public class ActiveTokensRepository : RepositoryBase<ActiveTokenDto>, IActiveTokensRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		public ActiveTokensRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings) : 
			base(clientProvider, settings, "active_tokens")
		{
			
		}

		#endregion
	}
}