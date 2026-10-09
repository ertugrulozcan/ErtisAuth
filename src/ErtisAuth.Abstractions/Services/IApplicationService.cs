using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Abstractions.Services;

public interface IApplicationService : IMembershipBoundedCrudService<Application>
{
	ValueTask<Application?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
	
	Task<Application?> GetBySlugAsync(string slug, string membershipId, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Creates the application with a newly generated secret; the plain secret is only returned here.
	/// </summary>
	Task<ApplicationWithSecret> CreateWithSecretAsync(Application model, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Replaces the application secret with a newly generated one; the previous secret is revoked immediately.
	/// </summary>
	Task<ApplicationWithSecret> RotateSecretAsync(string id, string membershipId, Utilizer utilizer, CancellationToken cancellationToken = default);
}