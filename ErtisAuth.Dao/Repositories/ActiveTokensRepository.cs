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
		public ActiveTokensRepository(IDatabaseSettings settings) : base(settings, "active_tokens", null)
		{
			
		}

		#endregion
	}
}