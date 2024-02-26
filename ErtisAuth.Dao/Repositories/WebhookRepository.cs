using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Webhooks;

namespace ErtisAuth.Dao.Repositories
{
	public class WebhookRepository : RepositoryBase<WebhookDto>, IWebhookRepository
	{
		#region Properties
        
		protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
		{
			new SingleIndexDefinition("name"),
			new SingleIndexDefinition("event"),
			new SingleIndexDefinition("status"),
			new SingleIndexDefinition("membership_id"),
		};

		#endregion
		
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public WebhookRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
			base(clientProvider, settings, "webhooks", actionBinder)
		{
			
		}

		#endregion
	}
}