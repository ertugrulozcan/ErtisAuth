using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Memberships;
using ErtisAuth.Core.Models.Setup;
using ErtisAuth.Core.Models.Users;

namespace ErtisAuth.Abstractions.Services;

/// <summary>
/// The one-time setup of a fresh installation: the first membership, its administrator role, user type and user, and
/// optionally an application.
/// </summary>
public interface ISetupService
{
	Task<bool> IsSetUpAsync(CancellationToken cancellationToken = default);
	
	Task<SetupResult> SetupAsync(string? setupToken, Membership membership, UserWithPassword user, Application? application, CancellationToken cancellationToken = default);
}