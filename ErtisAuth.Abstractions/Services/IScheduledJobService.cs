using System.Threading;
using System.Threading.Tasks;

namespace ErtisAuth.Abstractions.Services
{
	public interface IScheduledJobService
	{
		ValueTask ScheduleTokenCleanerJobsAsync(CancellationToken cancellationToken = default);
	}
}