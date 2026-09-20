using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Dto.Models.Events;

namespace ErtisAuth.Dao.Repositories;

public class EventRepository : RepositoryBase<EventDto>, IEventRepository
{
	#region Properties
    
	protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
	{
		new SingleIndexDefinition("event_type"),
		new SingleIndexDefinition("utilizer_id"),
		new SingleIndexDefinition("event_time"),
		new SingleIndexDefinition("membership_id")
	};
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	public EventRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings) : 
		base(clientProvider, settings, "events")
	{
		
	}
	
	#endregion
}