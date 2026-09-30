using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services;

/// <summary>
/// Keeps the unique indexes of the users collection in sync with the unique fields of the user types.
/// </summary>
public interface IUserUniqueIndexSynchronizer
{
	/// <summary>
	/// Creates the missing indexes for the given user types of the membership, without dropping any.
	/// Throws UniqueFieldHasDuplicates when the users already have duplicate values for a field to be unique.
	/// </summary>
	Task EnsureIndexesAsync(IEnumerable<UserType> userTypes, string membershipId, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Creates the missing indexes and drops the obsolete ones for the saved user types of the membership. Failures are logged, not thrown.
	/// </summary>
	Task SynchronizeAsync(string membershipId, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Synchronizes the indexes of all memberships (on startup). Failures are logged, not thrown.
	/// </summary>
	Task SynchronizeAllAsync(CancellationToken cancellationToken = default);
}
