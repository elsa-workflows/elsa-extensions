using System.Globalization;
using Elsa.Common;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Models;
using Elsa.Scheduling.Quartz.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// Default implementation of <see cref="IQuartzJobRetryScheduler"/>. Rather than retrying in-process, each retry is
/// scheduled as a new Quartz trigger, so that a pending retry survives an application restart and does not occupy a
/// Quartz worker thread while waiting.
/// </summary>
public class QuartzJobRetryScheduler(
    ISystemClock systemClock,
    IOptions<QuartzJobOptions> options,
    IQuartzRetryDelayCalculator delayCalculator,
    ITransientExceptionDetector transientExceptionDetector,
    ILogger<QuartzJobRetryScheduler> logger) : IQuartzJobRetryScheduler
{
    /// <inheritdoc />
    public bool IsRetryable(Exception exception)
    {
        var isRetryable = options.Value.IsRetryable;
        return isRetryable != null ? isRetryable(exception) : transientExceptionDetector.IsTransient(exception);
    }

    /// <inheritdoc />
    public async Task<bool> ScheduleRetryAsync(IJobExecutionContext context, Exception exception, CancellationToken cancellationToken = default)
    {
        var jobOptions = options.Value;
        var jobKey = context.JobDetail.Key;

        if (!jobOptions.RetryEnabled)
        {
            logger.LogDebug("Retries are disabled. Not rescheduling job {JobKey}", jobKey);
            return false;
        }

        var attemptNumber = context.GetRetryAttempt() + 1;

        if (attemptNumber > jobOptions.MaxRetryAttempts)
            return false;

        var delay = GetDelay(context, exception, attemptNumber, jobOptions);
        var retryTrigger = CreateRetryTrigger(context, attemptNumber, delay);

        logger.LogWarning(
            exception,
            "Job {JobKey} failed with a retryable error. Scheduling retry {AttemptNumber} of {MaxRetryAttempts} in {RetryDelay}",
            jobKey,
            attemptNumber,
            jobOptions.MaxRetryAttempts,
            delay);

        await context.Scheduler.RescheduleJob(context.Trigger.Key, retryTrigger, cancellationToken);
        return true;
    }

    private TimeSpan GetDelay(IJobExecutionContext context, Exception exception, int attemptNumber, QuartzJobOptions jobOptions)
    {
        var computedDelay = delayCalculator.CalculateDelay(attemptNumber, jobOptions);
        var delayGenerator = jobOptions.DelayGenerator;

        if (delayGenerator == null)
            return computedDelay;

        var retryContext = new QuartzJobRetryContext
        {
            JobExecutionContext = context,
            Exception = exception,
            AttemptNumber = attemptNumber,
            MaxRetryAttempts = jobOptions.MaxRetryAttempts,
            ComputedDelay = computedDelay
        };

        var customDelay = delayGenerator(retryContext);

        if (customDelay == null)
            return computedDelay;

        return customDelay.Value > TimeSpan.Zero ? customDelay.Value : TimeSpan.Zero;
    }

    private ITrigger CreateRetryTrigger(IJobExecutionContext context, int attemptNumber, TimeSpan delay)
    {
        // Carry over the job data of the failed trigger so that the workflow inputs it carries are not lost, and keep
        // the original trigger key so that the retry remains addressable (for example, to unschedule it).
        var jobDataMap = new JobDataMap();
        var triggerJobDataMap = context.Trigger.JobDataMap;

        if (triggerJobDataMap != null)
            jobDataMap.PutAll(triggerJobDataMap);

        jobDataMap[QuartzJobDataKeys.RetryAttempt] = attemptNumber.ToString(CultureInfo.InvariantCulture);

        return TriggerBuilder.Create()
            .ForJob(context.JobDetail.Key)
            .WithIdentity(context.Trigger.Key)
            .UsingJobData(jobDataMap)
            .StartAt(systemClock.UtcNow.Add(delay))
            .Build();
    }
}
