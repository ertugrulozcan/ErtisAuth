using System.Text.RegularExpressions;
using ErtisAuth.Abstractions.Services;
using ErtisAuth.Core.Exceptions;
using ErtisAuth.Core.Models.Users;
using ErtisAuth.Dao.Repositories.Interfaces;
using ErtisAuth.Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace ErtisAuth.Infrastructure.Services;

/// <summary>
/// Keeps the unique indexes of the users collection in sync with the unique fields of the user types
/// (see <see cref="UniqueFieldIndexHelper"/> for the indexes). An index is created before the user type change is saved,
/// so the index build itself is the duplicate check: the change is rejected when the users already have duplicates.
/// The obsolete indexes are dropped after the change is saved (or failed).
/// </summary>
public partial class UserUniqueIndexSynchronizer : IUserUniqueIndexSynchronizer
{
	#region Constants
	
	private const int DuplicateKeyErrorCode = 11000;
	
	#endregion
	
	#region Services
	
	private readonly IUserRepository _userRepository;
	private readonly IUserTypeRepository _userTypeRepository;
	private readonly ILogger<UserUniqueIndexSynchronizer> _logger;
	
	#endregion
	
	#region Constructors
	
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="userRepository"></param>
	/// <param name="userTypeRepository">Used directly instead of IUserTypeService, which depends on this service</param>
	/// <param name="logger"></param>
	public UserUniqueIndexSynchronizer(IUserRepository userRepository, IUserTypeRepository userTypeRepository, ILogger<UserUniqueIndexSynchronizer> logger)
	{
		this._userRepository = userRepository;
		this._userTypeRepository = userTypeRepository;
		this._logger = logger;
	}
	
	#endregion
	
	#region Methods
	
	public async Task EnsureIndexesAsync(IEnumerable<UserType> userTypes, string membershipId, CancellationToken cancellationToken = default)
	{
		var desiredIndexes = UniqueFieldIndexHelper.GetMembershipIndexes(userTypes, membershipId);
		var currentIndexNames = await this._userRepository.GetIndexNamesAsync(UniqueFieldIndexHelper.GetMembershipIndexNamePrefix(membershipId), cancellationToken: cancellationToken);
		foreach (var index in desiredIndexes.Where(x => !currentIndexNames.Contains(x.Name)))
		{
			await this.CreateIndexAsync(index, cancellationToken: cancellationToken);
		}
	}
	
	public async Task SynchronizeAsync(string membershipId, CancellationToken cancellationToken = default)
	{
		try
		{
			var userTypes = (await this._userTypeRepository.FindAsync(x => x.MembershipId == membershipId, sorting: null, cancellationToken: cancellationToken)).Items.ToArray();
			var currentIndexNames = await this._userRepository.GetIndexNamesAsync(UniqueFieldIndexHelper.GetMembershipIndexNamePrefix(membershipId), cancellationToken: cancellationToken);
			await this.SynchronizeAsync(UniqueFieldIndexHelper.GetMembershipIndexes(userTypes, membershipId), currentIndexNames, cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "The unique indexes of the users of membership '{MembershipId}' could not be synchronized", membershipId);
		}
	}
	
	public async Task SynchronizeAllAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			var currentIndexNames = await this._userRepository.GetIndexNamesAsync(UniqueFieldIndexHelper.IndexNamePrefix, cancellationToken: cancellationToken);
			var globalIndexes = UniqueFieldIndexHelper.GetGlobalIndexes();
			foreach (var index in globalIndexes.Where(x => !currentIndexNames.Contains(x.Name)))
			{
				await this.TryCreateIndexAsync(index, cancellationToken: cancellationToken);
			}
			
			var userTypes = (await this._userTypeRepository.FindAsync(sorting: null, cancellationToken: cancellationToken)).Items.ToArray();
			var userTypesByMembership = userTypes.GroupBy(x => x.MembershipId).ToDictionary(x => x.Key, x => x.ToArray());
			
			// Memberships without user types may still have the indexes of their deleted user types
			var currentIndexNamesByMembership = currentIndexNames
				.Select(x => (Name: x, MembershipId: UniqueFieldIndexHelper.TryParseIndexName(x, out var membershipId, out _) ? membershipId : null))
				.Where(x => x.MembershipId != null)
				.GroupBy(x => x.MembershipId!)
				.ToDictionary(x => x.Key, x => x.Select(y => y.Name).ToArray());
			
			foreach (var membershipId in userTypesByMembership.Keys.Union(currentIndexNamesByMembership.Keys))
			{
				var desiredIndexes = UniqueFieldIndexHelper.GetMembershipIndexes(userTypesByMembership.GetValueOrDefault(membershipId) ?? [], membershipId);
				await this.SynchronizeAsync(desiredIndexes, currentIndexNamesByMembership.GetValueOrDefault(membershipId) ?? [], cancellationToken: cancellationToken);
			}
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "The unique indexes of the users could not be synchronized");
		}
	}
	
	private async Task SynchronizeAsync(IReadOnlyList<UniqueFieldIndex> desiredIndexes, string[] currentIndexNames, CancellationToken cancellationToken = default)
	{
		foreach (var index in desiredIndexes.Where(x => !currentIndexNames.Contains(x.Name)))
		{
			await this.TryCreateIndexAsync(index, cancellationToken: cancellationToken);
		}
		
		foreach (var indexName in currentIndexNames.Where(x => desiredIndexes.All(y => y.Name != x)))
		{
			try
			{
				await this._userRepository.DropIndexAsync(indexName, cancellationToken: cancellationToken);
				this._logger.LogInformation("Unique index '{IndexName}' dropped from users collection", indexName);
			}
			catch (Exception ex)
			{
				this._logger.LogError(ex, "Unique index '{IndexName}' could not be dropped from users collection", indexName);
			}
		}
	}
	
	private async Task TryCreateIndexAsync(UniqueFieldIndex index, CancellationToken cancellationToken = default)
	{
		try
		{
			await this.CreateIndexAsync(index, cancellationToken: cancellationToken);
		}
		catch (Exception ex)
		{
			this._logger.LogError(ex, "Unique index '{IndexName}' could not be created on users collection", index.Name);
		}
	}
	
	private async Task CreateIndexAsync(UniqueFieldIndex index, CancellationToken cancellationToken = default)
	{
		try
		{
			await this._userRepository.CreateUniqueIndexAsync(index.Name, index.Path, index.PartialFilterExpression, cancellationToken: cancellationToken);
			this._logger.LogInformation("Unique index '{IndexName}' created on users collection", index.Name);
		}
		catch (MongoCommandException ex) when (ex.Code == DuplicateKeyErrorCode)
		{
			throw ErtisAuthException.UniqueFieldHasDuplicates(index.Path, GetDuplicateKey(ex.Message));
		}
	}
	
	/// <summary>
	/// The duplicate value in the error message (e.g. "dup key: { membership_id: "...", code: "A-1" }").
	/// </summary>
	private static string GetDuplicateKey(string message)
	{
		var match = DuplicateKeyRegex().Match(message);
		return match.Success ? match.Value : "duplicate key";
	}
	
	[GeneratedRegex(@"dup key: \{.*?\}")]
	private static partial Regex DuplicateKeyRegex();
	
	#endregion
}
