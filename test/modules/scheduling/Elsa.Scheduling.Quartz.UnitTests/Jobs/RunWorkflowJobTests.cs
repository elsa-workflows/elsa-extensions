using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.Options;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly Mock<IOptions<QuartzJobOptions>> _options = QuartzJobTestHelper.CreateQuartzJobOptions();
    private readonly Mock<ILogger<RunWorkflowJob>> _logger = new();
    private readonly RunWorkflowJob _job;

    public RunWorkflowJobTests()
    {
        _job = new RunWorkflowJob(
            _tenantAccessor.Object,
            _tenantFinder.Object,
            _workflowStarter.Object,
            [_transientDetector.Object],
            _options.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Execute_SuccessfulStart_CallsWorkflowStarter()
    {
        var (context, _) = CreateJobExecutionContext();
        _workflowStarter.SetupStartWorkflow(new StartWorkflowResponse { WorkflowInstanceId = "workflow-123" });

        await _job.Execute(context);

        _workflowStarter.VerifyStartWorkflowCalled();
    }

    [Fact]
    public async Task Execute_CannotStart_LogsWarningAndReturns()
    {
        var (context, _) = CreateJobExecutionContext();
        _workflowStarter.SetupStartWorkflow(new StartWorkflowResponse { CannotStart = true });

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

    [Fact]
    public async Task Execute_TransientException_ReschedulesJob()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        _transientDetector.SetupIsTransient(true);
        _workflowStarter.SetupStartWorkflowThrows(new TimeoutException());

        await _job.Execute(context);

        scheduler.VerifyRescheduled();
    }

    [Fact]
    public async Task Execute_NonTransientException_DeletesJob()
    {
        var (context, scheduler) = CreateJobExecutionContext();
        _transientDetector.SetupIsTransient(false);
        _workflowStarter.SetupStartWorkflowThrows(new InvalidOperationException());

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
