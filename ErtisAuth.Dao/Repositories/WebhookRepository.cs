using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Webhooks;
using MongoDB.Driver.Core.Events;

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
		/// <param name="eventSubscriber"></param>
		public WebhookRepository(IDatabaseSettings settings, IClientSettings clientSettings, IRepositoryActionBinder actionBinder, IEventSubscriber eventSubscriber) : 
			base(settings, "webhooks", clientSettings, actionBinder, eventSubscriber)
		{
			
		}

		#endregion
	}
}