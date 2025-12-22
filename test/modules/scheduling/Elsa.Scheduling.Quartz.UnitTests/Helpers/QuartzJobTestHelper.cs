using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.Options;
using Moq;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.UnitTests.Helpers;

/// <summary>
/// Provides common helper methods for testing Quartz jobs.
/// </summary>
public static class QuartzJobTestHelper
{
    /// <summary>
    /// Creates a mock job execution context with the specified job data.
    /// </summary>
    public static (IJobExecutionContext Context, Mock<QuartzScheduler> Scheduler) CreateJobExecutionContext(
        IDictionary<string, object> jobData)
    {
        var jobDataMap = new JobDataMap(jobData);
        var jobKey = new JobKey("test-job");
        var triggerKey = new TriggerKey("test-trigger");

        var jobDetail = new Mock<IJobDetail>();
        jobDetail.Setup(j => j.Key).Returns(jobKey);
        jobDetail.Setup(j => j.JobDataMap).Returns(jobDataMap);

        var trigger = new Mock<ITrigger>();
        trigger.Setup(t => t.Key).Returns(triggerKey);
        trigger.Setup(t => t.JobKey).Returns(jobKey);

        var scheduler = new Mock<QuartzScheduler>();
        scheduler.Setup(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTimeOffset.Now);
        scheduler.Setup(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var context = new Mock<IJobExecutionContext>();
        context.Setup(c => c.MergedJobDataMap).Returns(jobDataMap);
        context.Setup(c => c.JobDetail).Returns(jobDetail.Object);
        context.Setup(c => c.Trigger).Returns(trigger.Object);
        context.Setup(c => c.Scheduler).Returns(scheduler.Object);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        return (context.Object, scheduler);
    }

    /// <summary>
    /// Creates a mock for QuartzJobOptions with the specified retry delay.
    /// </summary>
    public static Mock<IOptions<QuartzJobOptions>> CreateQuartzJobOptions(int retryDelaySeconds = 10)
    {
        var options = new Mock<IOptions<QuartzJobOptions>>();
        options.Setup(o => o.Value).Returns(new QuartzJobOptions { TransientRetryDelaySeconds = retryDelaySeconds });
        return options;
    }

    /// <summary>
    /// Creates a mock tenant accessor that allows context pushing.
    /// </summary>
    public static Mock<ITenantAccessor> CreateTenantAccessor()
    {
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.PushContext(It.IsAny<Tenant>())).Returns((IDisposable)null!);
        return tenantAccessor;
    }

    /// <summary>
    /// Sets up a workflow starter to return the specified response.
    /// </summary>
    public static void SetupStartWorkflow(this Mock<IWorkflowStarter> workflowStarter, StartWorkflowResponse response) =>
        workflowStarter.Setup(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

    /// <summary>
    /// Sets up a workflow starter to throw the specified exception.
    /// </summary>
    public static void SetupStartWorkflowThrows(this Mock<IWorkflowStarter> workflowStarter, Exception exception) =>
        workflowStarter.Setup(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    /// <summary>
    /// Sets up a transient exception detector to return the specified value for any exception.
    /// </summary>
    public static void SetupIsTransient(this Mock<ITransientExceptionDetector> detector, bool isTransient) =>
        detector.Setup(d => d.IsTransient(It.IsAny<Exception>())).Returns(isTransient);

    /// <summary>
    /// Sets up a workflow runtime to return the specified workflow client.
    /// </summary>
    public static void SetupCreateClient(this Mock<IWorkflowRuntime> workflowRuntime, Mock<IWorkflowClient> workflowClient) =>
        workflowRuntime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowClient.Object);

    /// <summary>
    /// Sets up a workflow runtime to throw the specified exception.
    /// </summary>
    public static void SetupCreateClientThrows(this Mock<IWorkflowRuntime> workflowRuntime, Exception exception) =>
        workflowRuntime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

    /// <summary>
    /// Sets up a workflow client to return the specified response.
    /// </summary>
    public static void SetupRunInstance(this Mock<IWorkflowClient> workflowClient, RunWorkflowInstanceResponse? response = null) =>
        workflowClient.Setup(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response ?? new RunWorkflowInstanceResponse());

    /// <summary>
    /// Verifies that the scheduler rescheduled a job exactly once.
    /// </summary>
    public static void VerifyRescheduled(this Mock<QuartzScheduler> scheduler) =>
        scheduler.Verify(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>
    /// Verifies that the scheduler deleted a job exactly once.
    /// </summary>
    public static void VerifyDeleted(this Mock<QuartzScheduler> scheduler) =>
        scheduler.Verify(s => s.DeleteJob(It.IsAny<JobKey>(), It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>
    /// Verifies that the workflow starter was called exactly once.
    /// </summary>
    public static void VerifyStartWorkflowCalled(this Mock<IWorkflowStarter> workflowStarter) =>
        workflowStarter.Verify(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()), Times.Once);

    /// <summary>
    /// Verifies that the workflow client was called exactly once.
    /// </summary>
    public static void VerifyRunInstanceCalled(this Mock<IWorkflowClient> workflowClient) =>
        workflowClient.Verify(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()), Times.Once);
}
