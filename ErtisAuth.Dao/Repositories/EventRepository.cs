using Ertis.Data.Repository;
using Ertis.MongoDB.Client;
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
		/// <param name="clientProvider"></param>
		/// <param name="settings"></param>
		/// <param name="actionBinder"></param>
		public EventRepository(IMongoClientProvider clientProvider, IDatabaseSettings settings, IRepositoryActionBinder actionBinder) : 
			base(clientProvider, settings, "events", actionBinder)
		{
			
		}

		#endregion
	}
}