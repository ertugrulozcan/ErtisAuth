using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using ErtisAuth.Core.Models.Setup;
using ErtisAuth.Dao.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace ErtisAuth.Dao.Repositories;

public class SetupTokenRepository : RepositoryBase<SetupToken>, ISetupTokenRepository
{
	#region Constants
	
	public const string CollectionNameValue = "setup";
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="clientProvider"></param>
	/// <param name="settings"></param>
	/// <param name="logger"></param>
	public SetupTokenRepository(
		IMongoClientProvider clientProvider, 
		IDatabaseSettings settings,
		ILogger<SetupTokenRepository> logger) : 
		base(clientProvider, settings, logger, CollectionNameValue)
	{
	
	}
	
	#endregion
	
	#region Methods
	
	/// <summary>
	/// The collection only exists between the operator inserting the token and the completion of the setup.
	/// </summary>
	public async Task DropAsync(CancellationToken cancellationToken = default)
	{
		await this.Collection.Database.DropCollectionAsync(this.CollectionName, cancellationToken);
	}
	
	#endregion
}