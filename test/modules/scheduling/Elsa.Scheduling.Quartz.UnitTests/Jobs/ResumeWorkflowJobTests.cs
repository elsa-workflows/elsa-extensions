using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.UnitTests.Jobs;

public class ResumeWorkflowJobTests
{
    private readonly Mock<IWorkflowRuntime> _workflowRuntime = new();
    private readonly Mock<IJsonSerializer> _jsonSerializer = new();
    private readonly Mock<ITenantFinder> _tenantFinder = new();
    private readonly Mock<ITenantAccessor> _tenantAccessor = QuartzJobTestHelper.CreateTenantAccessor();
    private readonly Mock<ITransientExceptionDetector> _transientDetector = new();
    private readonly Mock<IQuartzJobRetryScheduler> _retryScheduler = new();
    private readonly Mock<ILogger<ResumeWorkflowJob>> _logger = new();
    private readonly ResumeWorkflowJob _job;

    public ResumeWorkflowJobTests()
    {
        _job = new(
            _workflowRuntime.Object,
            _jsonSerializer.Object,
            _tenantFinder.Object,
            _tenantAccessor.Object,
            _transientDetector.Object,
            _retryScheduler.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Execute_SuccessfulResume_CallsWorkflowClient()
    {
        var (context, _) = CreateJobExecutionContext();
        var workflowClient = new Mock<IWorkflowClient>();
        workflowClient.SetupRunInstance();
        _workflowRuntime.SetupCreateClient(workflowClient);

        await _job.Execute(context);

        workflowClient.VerifyRunInstanceCalled();
    }

    [Theory]
    [InlineData(typeof(HttpRequestException), true)]
    [InlineData(typeof(TimeoutException), true)]
    public async Task Execute_TransientException_ReschedulesJob(Type exceptionType, bool isTransient)
    {
        var (context, _) = CreateJobExecutionContext();
        _transientDetector.SetupIsTransient(isTransient);
        _workflowRuntime.SetupCreateClientThrows((Exception)Activator.CreateInstance(exceptionType)!);

        await _job.Execute(context);

        _retryScheduler.Verify(x => x.ScheduleRetryAsync(context, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(typeof(InvalidOperationException))]
    [InlineData(typeof(ArgumentException))]
    public async Task Execute_NonTransientException_DeletesJob(Type exceptionType)
    {
        var (context, scheduler) = CreateJobExecutionContext();
        _transientDetector.SetupIsTransient(false);
        _workflowRuntime.SetupCreateClientThrows((Exception)Activator.CreateInstance(exceptionType)!);

        await _job.Execute(context);

        scheduler.VerifyDeleted();
    }

    [Fact]
    public async Task Execute_UsesCorrectWorkflowInstanceId()
    {
        var (context, _) = CreateJobExecutionContext();
        var workflowClient = new Mock<IWorkflowClient>();
        workflowClient.Setup(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RunWorkflowInstanceResponse());

        string? capturedInstanceId = null;
        _workflowRuntime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, CancellationToken>((id, _) => capturedInstanceId = id)
            .ReturnsAsync(workflowClient.Object);

        await _job.Execute(context);

        Assert.Equal("workflow-instance-123", capturedInstanceId);
    }

    [Fact]
    public async Task Execute_WithActivityHandle_DeserializesCorrectly()
    {
        var activityHandle = new ActivityHandle { ActivityId = "activity-123" };
        var serializedHandle = "{\"ActivityId\":\"activity-123\"}";
        var (context, _) = CreateJobExecutionContext(serializedHandle);

        _jsonSerializer.Setup(j => j.Deserialize<ActivityHandle>(serializedHandle))
            .Returns(activityHandle);

        var workflowClient = new Mock<IWorkflowClient>();
        RunWorkflowInstanceRequest? capturedRequest = null;
        workflowClient.Setup(c => c.RunInstanceAsync(It.IsAny<RunWorkflowInstanceRequest>(), It.IsAny<CancellationToken>()))
            .Callback<RunWorkflowInstanceRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new RunWorkflowInstanceResponse());

        _workflowRuntime.Setup(r => r.CreateClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflowClient.Object);

        await _job.Execute(context);

        Assert.NotNull(capturedRequest);
        Assert.Equal("activity-123", capturedRequest.ActivityHandle?.ActivityId);
    }

    private static (IJobExecutionContext, Mock<QuartzScheduler>) CreateJobExecutionContext(string? activityHandle = null)
    {
        var jobData = new Dictionary<string, object>
        {
            { "WorkflowInstanceId", "workflow-instance-123" },
            { "BookmarkId", "bookmark-123" }
        };

        if (activityHandle != null)
            jobData.Add("ActivityHandle", activityHandle);

        return QuartzJobTestHelper.CreateJobExecutionContext(jobData);
    }
}
