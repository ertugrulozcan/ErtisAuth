using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Identity;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SortDirection = Ertis.Core.Collections.SortDirection;

namespace ErtisAuth.Dao.Repositories;

public class OneTimePasswordRepository : RepositoryBase<OneTimePassword>, IOneTimePasswordRepository
{
    #region Properties
    
    protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
    {
        new SingleIndexDefinition("user_id"),
		new CompoundIndexDefinition("email_address", "membership_id"),
		new CompoundIndexDefinition("username", "membership_id"),
		new TTLIndexDefinition("expire_time", SortDirection.Ascending, TTLGracePeriod)
    };
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="clientProvider"></param>
    /// <param name="settings"></param>
    /// <param name="logger"></param>
    public OneTimePasswordRepository(
        IMongoClientProvider clientProvider, 
        IDatabaseSettings settings,
        ILogger<OneTimePasswordRepository> logger) : 
        base(clientProvider, settings, logger, "otps")
    {
        
    }
    
    #endregion
	
	#region Attempt Methods
	
	public async Task<OneTimePassword?> TryReserveAttemptAsync(string id, int maxAttempts, CancellationToken cancellationToken = default)
	{
		// The limit is part of the filter, so the counter never goes beyond it, even with parallel requests
		// ReSharper disable once RedundantTypeArgumentsOfMethod
		return await this.Collection.FindOneAndUpdateAsync<OneTimePassword>(
			x => x.Id == id && x.FailedAttempts < maxAttempts,
			Builders<OneTimePassword>.Update.Inc(x => x.FailedAttempts, 1),
			new FindOneAndUpdateOptions<OneTimePassword> { ReturnDocument = ReturnDocument.After },
			cancellationToken);
	}
	
	public async Task ReleaseAttemptAsync(string id, CancellationToken cancellationToken = default)
	{
		await this.Collection.UpdateOneAsync(
			x => x.Id == id && x.FailedAttempts > 0,
			Builders<OneTimePassword>.Update.Inc(x => x.FailedAttempts, -1),
			cancellationToken: cancellationToken);
	}
	
	#endregion
}