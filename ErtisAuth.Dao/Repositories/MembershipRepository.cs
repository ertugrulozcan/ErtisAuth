using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Memberships;

namespace ErtisAuth.Dao.Repositories
{
	public class MembershipRepository : RepositoryBase<MembershipDto>, IMembershipRepository
	{
		#region Properties
        
		protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
		{
			new SingleIndexDefinition("name")
		};

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public MembershipRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
			base(clientProvider, settings, "memberships", actionBinder)
		{
			
		}

		#endregion
	}
}