using Ertis.Data.Repository;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Events;

namespace ErtisAuth.Dao.Repositories
{
	public class EventRepository : RepositoryBase<EventDto>, IEventRepository
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