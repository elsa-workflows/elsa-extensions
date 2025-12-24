using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;

namespace Elsa.Scheduling.Quartz.Jobs;

/// <summary>
/// A job that resumes a workflow.
/// </summary>
public class ResumeWorkflowJob(
    IWorkflowRuntime workflowRuntime,
    IJsonSerializer jsonSerializer,
    ITenantFinder tenantFinder,
    ITenantAccessor tenantAccessor,
    ITransientExceptionDetector transientExceptionDetector,
    IOptions<QuartzJobOptions> options,
    ILogger<ResumeWorkflowJob> logger) : IJob
{
    /// <inheritdoc />
    public async Task Execute(IJobExecutionContext context)
    {
        var tenant = await context.GetTenantAsync(tenantFinder);
        using (tenantAccessor.PushContext(tenant))
        {
            var map = context.MergedJobDataMap;
            var serializedActivityHandle = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.ActivityHandle));
            var activityHandle = serializedActivityHandle != null! ? jsonSerializer.Deserialize<ActivityHandle>(serializedActivityHandle) : null;
            var workflowInstanceId = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.WorkflowInstanceId));
            var cancellationToken = context.CancellationToken;

            try
            {
                var workflowClient = await workflowRuntime.CreateClientAsync(workflowInstanceId, cancellationToken);
                var request = new RunWorkflowInstanceRequest
                {
                    BookmarkId = (string)map.Get(nameof(ScheduleExistingWorkflowInstanceRequest.BookmarkId)),
                    ActivityHandle = activityHandle,
                    Input = map.GetDictionary(nameof(ScheduleExistingWorkflowInstanceRequest.Input)),
                    Properties = map.GetDictionary(nameof(ScheduleExistingWorkflowInstanceRequest.Properties)),
                };
                await workflowClient.RunInstanceAsync(request, cancellationToken: cancellationToken);

                logger.LogInformation("Resumed workflow instance {WorkflowInstanceId}", workflowInstanceId);
            }
            catch (Exception e) when (transientExceptionDetector.IsTransient(e))
            {
                logger.LogWarning(e, "A transient error occurred while resuming workflow instance {WorkflowInstanceId}. Rescheduling job for retry", workflowInstanceId);
                await context.RescheduleForTransientRetryAsync(options, cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "An error occurred while resuming workflow instance {WorkflowInstanceId}", workflowInstanceId);
                await context.Scheduler.DeleteJob(context.JobDetail.Key, cancellationToken);
            }
        }
    }
}