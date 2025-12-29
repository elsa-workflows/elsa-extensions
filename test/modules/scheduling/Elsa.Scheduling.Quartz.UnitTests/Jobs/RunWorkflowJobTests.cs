using Elsa.Common.Multitenancy;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.UnitTests.Jobs;

public class RunWorkflowJobTests
{
    private readonly Mock<ITenantAccessor> _tenantAccessor = QuartzJobTestHelper.CreateTenantAccessor();
    private readonly Mock<ITenantFinder> _tenantFinder = new();
    private readonly Mock<IWorkflowStarter> _workflowStarter = new();
    private readonly Mock<ITransientExceptionDetector> _transientDetector = new();
    private readonly Mock<IQuartzJobRetryScheduler> _retryScheduler = new();
    private readonly Mock<ILogger<RunWorkflowJob>> _logger = new();
    private readonly RunWorkflowJob _job;

    public RunWorkflowJobTests()
    {
        _job = new(
            _tenantAccessor.Object,
            _tenantFinder.Object,
            _workflowStarter.Object,
            _transientDetector.Object,
            _retryScheduler.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Execute_SuccessfulStart_CallsWorkflowStarter()
    {
        var (context, _) = CreateJobExecutionContext();
        _workflowStarter.SetupStartWorkflow(new() { WorkflowInstanceId = "workflow-123" });

        await _job.Execute(context);

        _workflowStarter.VerifyStartWorkflowCalled();
    }

    [Fact]
    public async Task Execute_CannotStart_LogsWarningAndReturns()
    {
        var (context, _) = CreateJobExecutionContext();
        _workflowStarter.SetupStartWorkflow(new() { CannotStart = true });

        await _job.Execute(context);

        _workflowStarter.VerifyStartWorkflowCalled();
    }

    [Fact]
    public async Task Execute_WorkflowGraphNotFound_DeletesJob()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        var handle = WorkflowDefinitionHandle.ByDefinitionVersionId("workflow-def-123");
        _workflowStarter.SetupStartWorkflowThrows(new WorkflowGraphNotFoundException("Not found", handle));

        await _job.Execute(context);

        scheduler.VerifyDeleted();
    }

    [Theory]
    [InlineData(typeof(TimeoutException), true)]
    [InlineData(typeof(HttpRequestException), true)]
    public async Task Execute_TransientException_ReschedulesJob(Type exceptionType, bool isTransient)
    {
        var (context, _) = CreateJobExecutionContext();
        _transientDetector.SetupIsTransient(isTransient);
        _workflowStarter.SetupStartWorkflowThrows((Exception)Activator.CreateInstance(exceptionType)!);

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
        _workflowStarter.SetupStartWorkflowThrows((Exception)Activator.CreateInstance(exceptionType)!);

        await _job.Execute(context);

        scheduler.VerifyDeleted();
    }

    [Fact]
    public async Task Execute_UsesCorrectWorkflowDefinitionHandle()
    {
        var (context, _) = CreateJobExecutionContext();
        StartWorkflowRequest? capturedRequest = null;
        _workflowStarter.Setup(w => w.StartWorkflowAsync(It.IsAny<StartWorkflowRequest>(), It.IsAny<CancellationToken>()))
            .Callback<StartWorkflowRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new StartWorkflowResponse { WorkflowInstanceId = "workflow-123" });

        await _job.Execute(context);

        Assert.NotNull(capturedRequest);
        Assert.Equal("workflow-def-123", capturedRequest.WorkflowDefinitionHandle.DefinitionVersionId);
    }

    private static (IJobExecutionContext, Mock<QuartzScheduler>) CreateJobExecutionContext() =>
        QuartzJobTestHelper.CreateJobExecutionContext(new Dictionary<string, object>
        {
            { "DefinitionVersionId", "workflow-def-123" },
            { "CorrelationId", "corr-123" },
            { "TriggerActivityId", "trigger-123" }
        });
}
