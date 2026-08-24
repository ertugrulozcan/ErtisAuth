using ErtisAuth.Core.Models.Applications;

// ReSharper disable UnusedMember.Global
namespace ErtisAuth.Abstractions.Services;

public interface IApplicationService : IMembershipBoundedCrudService<Application>
{
	Application? GetById(string id);
	
	ValueTask<Application?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
	
	bool IsSystemReservedApplication(Application? application);
}