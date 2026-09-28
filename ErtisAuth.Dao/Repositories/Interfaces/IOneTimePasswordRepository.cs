using Ertis.MongoDB.Repository;
using ErtisAuth.Core.Models.Identity;

namespace ErtisAuth.Dao.Repositories.Interfaces;

public interface IOneTimePasswordRepository : IMongoRepository<OneTimePassword>
{
	/// <summary>
	/// Atomically counts one more attempt if fewer than <paramref name="maxAttempts"/> were counted.
	/// Returns the updated one-time password, or null when the attempts are used up or it no longer exists.
	/// </summary>
	Task<OneTimePassword?> TryReserveAttemptAsync(string id, int maxAttempts, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Atomically gives back an attempt reserved by <see cref="TryReserveAttemptAsync"/>.
	/// </summary>
	Task ReleaseAttemptAsync(string id, CancellationToken cancellationToken = default);
}