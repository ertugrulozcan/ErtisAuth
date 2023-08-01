using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Events;
using MongoDB.Driver.Core.Events;

namespace ErtisAuth.Dao.Repositories
{
	public class EventRepository : RepositoryBase<EventDto>, IEventRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="clientSettings"></param>
		/// <param name="actionBinder"></param>
		/// <param name="eventSubscriber"></param>
		public EventRepository(IDatabaseSettings settings, IClientSettings clientSettings, IRepositoryActionBinder actionBinder, IEventSubscriber eventSubscriber) : 
			base(settings, "events", clientSettings, actionBinder, eventSubscriber)
		{
			
		}

		#endregion
	}
}