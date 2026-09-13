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
/// scheduled as a separate one-shot Quartz trigger for the same job, so that a pending retry does not occupy a Quartz
/// worker thread while waiting, the original schedule (cron or repeating) is left intact, and the retry survives an
/// application restart when Quartz is configured with a persistent job store (with the default in-memory store,
/// pending retries are lost on restart).
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
            logger.LogDebug("Retries are disabled. Not scheduling a retry for job {JobKey}", jobKey);
            return false;
        }

        var attemptsMade = Math.Max(context.GetRetryAttempt(), 0);

        if (attemptsMade >= jobOptions.MaxRetryAttempts)
            return false;

        var attemptNumber = attemptsMade + 1;
        var delay = GetDelay(context, exception, attemptNumber, jobOptions);
        var retryTrigger = CreateRetryTrigger(context, attemptNumber, delay);

        logger.LogWarning(
            exception,
            "Job {JobKey} failed with a retryable error. Scheduling retry {AttemptNumber} of {MaxRetryAttempts} in {RetryDelay}",
            jobKey,
            attemptNumber,
            jobOptions.MaxRetryAttempts,
            delay);

        // Replace any existing pending retry rather than the firing trigger, so a cron or repeating schedule keeps
        // its next fire times. Unschedule-then-schedule covers both the first retry and a later attempt that reuses
        // the same derived key.
        await context.Scheduler.UnscheduleJob(retryTrigger.Key, cancellationToken);
        await context.Scheduler.ScheduleJob(retryTrigger, cancellationToken);

        return true;
    }

    /// <inheritdoc />
    public async Task CancelPendingRetryAsync(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        if (QuartzTriggerKeys.IsRetryTrigger(context.Trigger.Key))
            return;

        var retryKey = QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key);
        var cancelled = await context.Scheduler.UnscheduleJob(retryKey, cancellationToken);

        if (cancelled)
            logger.LogDebug("Cancelled pending retry trigger {RetryTriggerKey} for job {JobKey} because the original schedule fired again", retryKey, context.JobDetail.Key);
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
        // Carry over the job data of the failed trigger so that the workflow inputs it carries are not lost. Use a
        // derived key so the original trigger (and its cron / repeating schedule) is left in place.
        var jobDataMap = new JobDataMap();
        var triggerJobDataMap = context.Trigger.JobDataMap;

        if (triggerJobDataMap != null)
            jobDataMap.PutAll(triggerJobDataMap);

        jobDataMap[QuartzJobDataKeys.RetryAttempt] = attemptNumber.ToString(CultureInfo.InvariantCulture);

        var now = systemClock.UtcNow;
        var startAt = delay >= DateTimeOffset.MaxValue - now ? DateTimeOffset.MaxValue : now.Add(delay);

        return TriggerBuilder.Create()
            .ForJob(context.JobDetail.Key)
            .WithIdentity(QuartzTriggerKeys.GetRetryTriggerKey(context.Trigger.Key))
            .UsingJobData(jobDataMap)
            .StartAt(startAt)
            .Build();
    }
}
