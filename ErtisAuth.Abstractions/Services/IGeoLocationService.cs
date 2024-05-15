using System.Threading;
using System.Threading.Tasks;
using ErtisAuth.Core.Models.GeoLocation;

namespace ErtisAuth.Abstractions.Services
{
	public interface IGeoLocationService
    {
		Task<GeoLocationInfo> LookupAsync(string ipAddress, CancellationToken cancellationToken = default);
    }
}