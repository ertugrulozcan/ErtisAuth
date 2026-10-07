using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Events;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class EventRepository : RepositoryBase<ErtisAuthEvent>, IEventRepository
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
	/// <param name="logger"></param>
	public EventRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings,
		ILogger<EventRepository> logger) : 
		base(clientProvider, settings, logger, "events")
	{
		
	}
	
	#endregion
}