using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Repository;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Events;

namespace ErtisAuth.Dao.Repositories
{
	public class EventRepository : MongoRepositoryBase<EventDto>, IEventRepository
	{
		#region Constructors

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public EventRepository(IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : base(settings, "events", actionBinder)
		{
			
		}

		#endregion
	}
}