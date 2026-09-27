using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Core.Models.Identity;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Abstractions.Services;

public interface IApplicationService : IMembershipBoundedCrudService<Application>
{
	Application? GetById(string id);
	
	ValueTask<Application?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Creates the application with a newly generated secret; the plain secret is only returned here.
	/// </summary>
	Task<ApplicationWithSecret> CreateWithSecretAsync(Utilizer utilizer, string membershipId, Application model, CancellationToken cancellationToken = default);
	
	/// <summary>
	/// Replaces the application secret with a newly generated one; the previous secret is revoked immediately.
	/// </summary>
	Task<ApplicationWithSecret> RotateSecretAsync(Utilizer utilizer, string membershipId, string id, CancellationToken cancellationToken = default);
}