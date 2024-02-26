using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Identity;

namespace ErtisAuth.Dao.Repositories
{
	public class RevokedTokensRepository : RepositoryBase<RevokedTokenDto>, IRevokedTokensRepository
	{
		#region Properties
        
		protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
		{
			new SingleIndexDefinition("token.access_token")
		};

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		public RevokedTokensRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings) : 
			base(clientProvider, settings, "revoked_tokens")
		{
			
		}

		#endregion
	}
}