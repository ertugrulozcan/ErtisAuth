namespace ErtisAuth.Abstractions.Services;

public interface IScheduledJobService
{
	ValueTask ScheduleTokenCleanerJobsAsync(CancellationToken cancellationToken = default);
}