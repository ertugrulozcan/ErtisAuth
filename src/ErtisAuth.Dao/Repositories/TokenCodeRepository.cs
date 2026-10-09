using Ertis.Core.Collections;
using Ertis.MongoDB.Client;
using Ertis.MongoDB.Configuration;
using Ertis.MongoDB.Models;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Core.Models.Identity;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using SortDirection = Ertis.Core.Collections.SortDirection;

namespace ErtisAuth.Dao.Repositories;

public class TokenCodeRepository : RepositoryBase<TokenCode>, ITokenCodeRepository
{
	#region Properties
	
	protected override IIndexDefinition[] Indexes => new IIndexDefinition[]
	{
		new SingleIndexDefinition("user_id"),
		new SingleIndexDefinition("device_code_hash"),
		new SingleIndexDefinition("membership_id"),
		// Unique: two codes generated at the same time can't get the same user code
		new CompoundIndexDefinition("membership_id", "user_code") { IsUnique = true },
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
	public TokenCodeRepository(
		IMongoClientProvider clientProvider,
		IDatabaseSettings settings,
		ILogger<TokenCodeRepository> logger) :
		base(clientProvider, settings, logger, "codes")
	{
	
	}
	
	#endregion
	
	#region Atomic Methods
	
	public async Task<TokenCode?> TryDecideAsync(string userCode, string membershipId, string status, string userId, string[]? scopes, DateTime now, CancellationToken cancellationToken = default)
	{
		// ReSharper disable once RedundantTypeArgumentsOfMethod
		return await this.Collection.FindOneAndUpdateAsync<TokenCode>(
			x => x.UserCode == userCode && x.MembershipId == membershipId && x.Status == TokenCodeStatus.Pending && x.ExpireTime > now,
			Builders<TokenCode>.Update
				.Set(x => x.Status, status)
				.Set(x => x.UserId, userId)
				.Set(x => x.Scopes, scopes)
				.Set(x => x.DecidedAt, now),
			new FindOneAndUpdateOptions<TokenCode> { ReturnDocument = ReturnDocument.After },
			cancellationToken);
	}
	
	public async Task<bool> TryRegisterPollAsync(string id, int interval, DateTime now, CancellationToken cancellationToken = default)
	{
		var previousPollLimit = now.AddSeconds(-interval);
		var result = await this.Collection.UpdateOneAsync(
			x => x.Id == id && (x.LastPolledAt == null || x.LastPolledAt <= previousPollLimit),
			Builders<TokenCode>.Update.Set(x => x.LastPolledAt, now),
			cancellationToken: cancellationToken);
		
		return result.ModifiedCount > 0;
	}
	
	public async Task<TokenCode?> TryConsumeAsync(string id, CancellationToken cancellationToken = default)
	{
		// ReSharper disable once RedundantTypeArgumentsOfMethod
		return await this.Collection.FindOneAndDeleteAsync<TokenCode>(
			x => x.Id == id && x.Status == TokenCodeStatus.Approved,
			cancellationToken: cancellationToken);
	}
	
	#endregion
}
