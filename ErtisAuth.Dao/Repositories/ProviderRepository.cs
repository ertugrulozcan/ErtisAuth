using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Providers;

namespace ErtisAuth.Dao.Repositories
{
	public class ProviderRepository : RepositoryBase<ProviderDto>, IProviderRepository
	{
		#region Properties
        
		protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
		{
			new SingleIndexDefinition("isActive"),
			new SingleIndexDefinition("membership_id"),
			new CompoundIndexDefinition("isActive", "membership_id"),
		};

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public ProviderRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
			base(clientProvider, settings, "providers", actionBinder)
		{
			
		}

		#endregion
	}
}