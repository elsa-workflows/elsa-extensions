using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Microsoft.Extensions.Logging;
using Quartz;
using QuartzIScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.Services;

/// <summary>
/// An implementation of <see cref="IWorkflowScheduler"/> that uses Quartz.NET.
/// </summary>
public class QuartzWorkflowScheduler(ISchedulerFactory schedulerFactoryFactory, IJsonSerializer jsonSerializer, ITenantAccessor tenantAccessor, IJobKeyProvider jobKeyProvider, ILogger<QuartzWorkflowScheduler> logger) : IWorkflowScheduler
{
    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);

        var trigger = TriggerBuilder.Create()
            .ForJob(GetRunWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .StartAt(at)
            .Build();

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleAtAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset at, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(GetResumeWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .StartAt(at)
            .Build();

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .WithIdentity(GetTriggerKey(taskName))
            .ForJob(GetRunWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleRecurringAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, DateTimeOffset startAt, TimeSpan interval, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .WithIdentity(GetTriggerKey(taskName))
            .ForJob(GetResumeWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .StartAt(startAt)
            .WithSimpleSchedule(schedule => schedule.WithInterval(interval).RepeatForever())
            .Build();

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleNewWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .UsingJobData(CreateJobDataMap(request))
            .ForJob(GetRunWorkflowJobKey())
            .WithIdentity(GetTriggerKey(taskName))
            .WithCronSchedule(cronExpression)
            .Build();

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask ScheduleCronAsync(string taskName, ScheduleExistingWorkflowInstanceRequest request, string cronExpression, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var trigger = TriggerBuilder.Create()
            .ForJob(GetResumeWorkflowJobKey())
            .UsingJobData(CreateJobDataMap(request))
            .WithIdentity(GetTriggerKey(taskName))
            .WithCronSchedule(cronExpression).Build();

        await ScheduleJobAsync(scheduler, trigger, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask UnscheduleAsync(string taskName, CancellationToken cancellationToken = default)
    {
        var scheduler = await schedulerFactoryFactory.GetScheduler(cancellationToken);
        var triggerKey = GetTriggerKey(taskName);
        await scheduler.UnscheduleJob(triggerKey, cancellationToken);
    }
    
    private async Task ScheduleJobAsync(QuartzIScheduler scheduler, ITrigger trigger, CancellationToken cancellationToken)
    {
        try
        {
            // Try to schedule the trigger. In clustered mode, multiple instances may attempt this simultaneously.
            // The ScheduleJob method will throw ObjectAlreadyExistsException if a trigger with the same key already exists.
            // Unlike AddJob, ScheduleJob does not have a 'replace' parameter - it always fails if the trigger exists.
            // Note: To update an existing trigger, callers should first use UnscheduleAsync before scheduling the new trigger.
            await scheduler.ScheduleJob(trigger, cancellationToken);
        }
        catch (ObjectAlreadyExistsException)
        {
            // Trigger already exists. In clustered scenarios, this is an expected race condition
            // when multiple pods attempt to schedule the same trigger during tenant activation or startup.
            // We can safely ignore this and continue, as the trigger is already scheduled.
            logger.LogDebug("Trigger {TriggerKey} already exists, skipping scheduling. This is expected in clustered deployments during concurrent operations", trigger.Key);
        }
        catch (JobPersistenceException ex) when (ex.Message.Contains("does not exist"))
        {
            // The job referenced by the trigger doesn't exist yet. This can happen during startup in clustered mode
            // when triggers are being scheduled before the RegisterJobsTask has completed on all instances.
            // Ensure the job exists and retry scheduling the trigger.
            logger.LogDebug("Job {JobKey} does not exist yet, ensuring it is created before scheduling trigger {TriggerKey}", trigger.JobKey, trigger.Key);
            await EnsureJobExistsAsync(scheduler, trigger.JobKey, cancellationToken);
            await scheduler.ScheduleJob(trigger, cancellationToken);
        }
    }

    private async Task EnsureJobExistsAsync(QuartzIScheduler scheduler, JobKey jobKey, CancellationToken cancellationToken)
    {
        // Check if job already exists
        var jobExists = await scheduler.CheckExists(jobKey, cancellationToken);
        if (jobExists)
            return;

        // Determine the job type based on the job key
        var jobType = jobKey.Name switch
        {
            var name when name == jobKeyProvider.GetJobKey<RunWorkflowJob>().Name => typeof(RunWorkflowJob),
            var name when name == jobKeyProvider.GetJobKey<ResumeWorkflowJob>().Name => typeof(ResumeWorkflowJob),
            _ => throw new InvalidOperationException($"Unknown job type for job key: {jobKey}")
        };

        var job = JobBuilder.Create(jobType)
            .WithIdentity(jobKey)
            .StoreDurably()
            .Build();

        try
        {
            const bool replaceExisting = false;
            await scheduler.AddJob(job, replaceExisting, cancellationToken);
            logger.LogDebug("Created missing job {JobKey}", jobKey);
        }
        catch (ObjectAlreadyExistsException)
        {
            // Another instance created it between our check and add attempt - this is fine
            logger.LogDebug("Job {JobKey} was created by another instance", jobKey);
        }
    }

    private JobDataMap CreateJobDataMap(ScheduleNewWorkflowInstanceRequest request)
    {
        return new JobDataMap()
                .AddIfNotEmpty("TenantId", tenantAccessor.Tenant?.Id)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.CorrelationId), request.CorrelationId)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.WorkflowDefinitionHandle.DefinitionVersionId), request.WorkflowDefinitionHandle.DefinitionVersionId)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.TriggerActivityId), request.TriggerActivityId)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.ParentId), request.ParentId)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.Input), request.Input)
                .AddIfNotEmpty(nameof(ScheduleNewWorkflowInstanceRequest.Properties), request.Properties)
            ;
    }

    private JobDataMap CreateJobDataMap(ScheduleExistingWorkflowInstanceRequest request)
    {
        var serializedActivityHandle = request.ActivityHandle != null ? jsonSerializer.Serialize(request.ActivityHandle) : null;

        return new JobDataMap()
            .AddIfNotEmpty("TenantId", tenantAccessor.Tenant?.Id)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.WorkflowInstanceId), request.WorkflowInstanceId)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.Input), request.Input)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.Properties), request.Properties)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.ActivityHandle), serializedActivityHandle)
            .AddIfNotEmpty(nameof(ScheduleExistingWorkflowInstanceRequest.BookmarkId), request.BookmarkId);
    }

    private JobKey GetRunWorkflowJobKey() => jobKeyProvider.GetJobKey<RunWorkflowJob>();
    private JobKey GetResumeWorkflowJobKey() => jobKeyProvider.GetJobKey<ResumeWorkflowJob>();
    private string GetGroupName() => jobKeyProvider.GetGroupName();
    private TriggerKey GetTriggerKey(string taskName) => new(taskName, GetGroupName());
}