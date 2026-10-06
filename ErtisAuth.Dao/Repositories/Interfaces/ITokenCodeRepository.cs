using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface ITokenCodeRepository : IMongoRepository<TokenCode>
{
	/// <summary>
	/// Atomically approves or denies a pending, not expired code for the user.
	/// Returns the updated code, or null when there is no such code (unknown, expired or already decided).
	/// </summary>
	Task<TokenCode?> TryDecideAsync(string userCode, string membershipId, string status, string userId, DateTime now, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Atomically records a poll of the device if the previous one was at least <paramref name="interval"/> seconds ago.
	/// Returns false when the device polls too often (or the code no longer exists).
	/// </summary>
	Task<bool> TryRegisterPollAsync(string id, int interval, DateTime now, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Atomically deletes an approved code and returns it, so that only one request gets the token.
	/// Returns null when the code is not approved or no longer exists.
	/// </summary>
	Task<TokenCode?> TryConsumeAsync(string id, CancellationToken cancellationToken = default);
}
