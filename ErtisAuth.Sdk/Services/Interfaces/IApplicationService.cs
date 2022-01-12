using ErtisAuth.Core.Models.Applications;
using ErtisAuth.Sdk.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace ErtisAuth.Sdk.Services.Interfaces
{
	[ServiceLifetime(ServiceLifetime.Singleton)]
	public interface IApplicationService : IMembershipBoundedService<Application>
	{
		
	}
}