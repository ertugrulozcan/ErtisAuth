using System.Threading.Tasks;

namespace ErtisAuth.Abstractions.Services.Interfaces
{
	public interface IScheduledJobService
	{
		ValueTask ScheduleTokenCleanerJobsAsync();
	}
}