using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Applications;

namespace ErtisAuth.Dao.Repositories
{
	public class ApplicationRepository : RepositoryBase<ApplicationDto>, IApplicationRepository
	{
		#region Properties
        
		protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
		{
			new SingleIndexDefinition("name"),
			new SingleIndexDefinition("role"),
			new SingleIndexDefinition("membership_id"),
			new CompoundIndexDefinition("_id", "membership_id"),
			new CompoundIndexDefinition("name", "membership_id"),
		};

		#endregion
		
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