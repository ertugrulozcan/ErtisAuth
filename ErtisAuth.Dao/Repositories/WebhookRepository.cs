using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Webhooks;

namespace ErtisAuth.Dao.Repositories
{
	public class WebhookRepository : RepositoryBase<WebhookDto>, IWebhookRepository
	{
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