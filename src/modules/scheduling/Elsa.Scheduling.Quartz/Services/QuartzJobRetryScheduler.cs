using Elsa.Common;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Options;
using Microsoft.Extensions.Options;
using Quartz;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Default implementation of <see cref="IQuartzJobRetryScheduler"/>.
/// </summary>
public class QuartzJobRetryScheduler(ISystemClock systemClock, IOptions<QuartzJobOptions> options) : IQuartzJobRetryScheduler
{
    /// <inheritdoc />
    public async Task ScheduleRetryAsync(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var delay = options.Value.TransientExceptionRetryDelay;
        var now = systemClock.UtcNow;

        var trigger = TriggerBuilder.Create()
            .ForJob(context.JobDetail.Key)
            .StartAt(now.Add(delay))
            .Build();

        await context.Scheduler.RescheduleJob(context.Trigger.Key, trigger, cancellationToken);
    }
}

