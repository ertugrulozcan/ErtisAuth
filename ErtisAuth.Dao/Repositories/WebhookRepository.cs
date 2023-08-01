using Ertis.Data.Repository;
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
		/// <param name="settings"></param>
		/// <param name="clientSettings"></param>
		/// <param name="actionBinder"></param>
		public WebhookRepository(IDatabaseSettings settings, IClientSettings clientSettings, IRepositoryActionBinder actionBinder) : base(settings, "webhooks", clientSettings, actionBinder)
		{
			
		}

		#endregion
	}
}