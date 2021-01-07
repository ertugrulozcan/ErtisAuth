using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Webhooks;

namespace ErtisAuth.Dao.Repositories
{
	public class WebhookRepository : MongoRepositoryBase<WebhookDto>, IWebhookRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public WebhookRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "webhooks", actionBinder)
		{
			
		}

		#endregion
	}
}