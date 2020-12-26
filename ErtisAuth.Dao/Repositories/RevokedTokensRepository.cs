using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Dao.Repositories
{
	public class RevokedTokensRepository : MongoRepositoryBase<RevokedTokenDto>, IRevokedTokensRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		public RevokedTokensRepository(IDatabaseSettings settings) : base(settings, "revoked_tokens")
		{
			
		}

		#endregion
	}
}