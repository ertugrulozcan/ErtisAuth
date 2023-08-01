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
		/// <param name="settings"></param>
		/// <param name="clientSettings"></param>
		public ActiveTokensRepository(IDatabaseSettings settings, IClientSettings clientSettings) : base(settings, "active_tokens", clientSettings, null)
		{
			
		}

		#endregion
	}
}