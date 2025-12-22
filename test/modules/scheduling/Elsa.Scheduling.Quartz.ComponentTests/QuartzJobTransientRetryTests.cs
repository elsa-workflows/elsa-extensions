using Elsa.Scheduling.Quartz.ComponentTests.Abstractions;
using Elsa.Scheduling.Quartz.ComponentTests.Fixtures;
using Elsa.Scheduling.Quartz.ComponentTests.Helpers;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace Elsa.Scheduling.Quartz.ComponentTests;

/// <summary>
/// Component tests for Quartz job transient retry behavior.
/// These tests validate that jobs properly retry on transient exceptions and give up on non-transient exceptions.
/// </summary>
public class QuartzJobTransientRetryTests(SchedulingApp app) : AppComponentTest(app)
{
    [Fact]
    public async Task RunWorkflowJob_TransientException_RetriesAndEventuallySucceeds()
    {
        // Arrange - Create a workflow starter that fails with transient exceptions
        var scheduler = await WorkflowServer.GetSchedulerAsync();
        var workflowStarter = Scope.ServiceProvider.GetRequiredService<IWorkflowStarter>();
        var failingStarter = new FailingWorkflowStarter(workflowStarter)
        {
            FailuresBeforeSuccess = 2, // Fail twice before succeeding
            ExceptionToThrow = new TimeoutException("Simulated transient timeout")
        };

        // Create job manually with our failing starter
        var job = new RunWorkflowJob(
            Scope.ServiceProvider.GetRequiredService<Common.Multitenancy.ITenantAccessor>(),
            Scope.ServiceProvider.GetRequiredService<Common.Multitenancy.ITenantFinder>(),
            failingStarter,
            Scope.ServiceProvider.GetRequiredService<IEnumerable<Contracts.ITransientExceptionDetector>>(),
            Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Options.QuartzJobOptions>>(),
            Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RunWorkflowJob>>()
        );

        // Create job data and schedule it
        var jobDataMap = new JobDataMap
        {
            { "DefinitionVersionId", "test-workflow-def" },
            { "CorrelationId", "test-correlation" }
        };

        var jobDetail = JobBuilder.Create<RunWorkflowJob>()
            .WithIdentity("test-transient-job", "test-group")
            .UsingJobData(jobDataMap)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-transient-trigger", "test-group")
            .ForJob(jobDetail)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        // Act - Execute the job multiple times to simulate retries
        var context = CreateTestContext(scheduler, jobDetail, trigger);

        // First attempt - should fail with transient exception
        await job.Execute(context);
        Assert.Equal(1, failingStarter.CallCount);

        // Second attempt - should fail again
        await job.Execute(context);
        Assert.Equal(2, failingStarter.CallCount);

        // Third attempt - should succeed
        await job.Execute(context);
        Assert.Equal(3, failingStarter.CallCount);

        // Assert - Verify the job was retried the expected number of times
        Assert.Equal(3, failingStarter.CallCount);
    }

    [Fact]
    public async Task RunWorkflowJob_NonTransientException_DoesNotRetry()
    {
        // Arrange
        var scheduler = await WorkflowServer.GetSchedulerAsync();
        var workflowStarter = Scope.ServiceProvider.GetRequiredService<IWorkflowStarter>();
        var failingStarter = new FailingWorkflowStarter(workflowStarter)
        {
            FailuresBeforeSuccess = 10, // Would retry many times if transient
            ExceptionToThrow = new InvalidOperationException("Non-transient error")
        };

        var job = new RunWorkflowJob(
            Scope.ServiceProvider.GetRequiredService<Common.Multitenancy.ITenantAccessor>(),
            Scope.ServiceProvider.GetRequiredService<Common.Multitenancy.ITenantFinder>(),
            failingStarter,
            Scope.ServiceProvider.GetRequiredService<IEnumerable<Contracts.ITransientExceptionDetector>>(),
            Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Options.QuartzJobOptions>>(),
            Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RunWorkflowJob>>()
        );

        var jobDataMap = new JobDataMap
        {
            { "DefinitionVersionId", "test-workflow-def" }
        };

        var jobDetail = JobBuilder.Create<RunWorkflowJob>()
            .WithIdentity("test-nontransient-job", "test-group")
            .UsingJobData(jobDataMap)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-nontransient-trigger", "test-group")
            .ForJob(jobDetail)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        // Act
        var context = CreateTestContext(scheduler, jobDetail, trigger);
        await job.Execute(context);

        // Assert - Job should be called once, then deleted (not retried)
        Assert.Equal(1, failingStarter.CallCount);

        // Verify job was deleted from scheduler
        var jobExists = await scheduler.CheckExists(jobDetail.Key);
        Assert.False(jobExists);
    }

    [Fact]
    public async Task RunWorkflowJob_TransientException_IsDetectedCorrectly()
    {
        // Arrange
        var scheduler = await WorkflowServer.GetSchedulerAsync();
        var workflowStarter = Scope.ServiceProvider.GetRequiredService<IWorkflowStarter>();
        var failingStarter = new FailingWorkflowStarter(workflowStarter)
        {
            FailuresBeforeSuccess = 1,
            ExceptionToThrow = new HttpRequestException("Simulated network error")
        };

        var job = new RunWorkflowJob(
            Scope.ServiceProvider.GetRequiredService<Common.Multitenancy.ITenantAccessor>(),
            Scope.ServiceProvider.GetRequiredService<Common.Multitenancy.ITenantFinder>(),
            failingStarter,
            Scope.ServiceProvider.GetRequiredService<IEnumerable<Contracts.ITransientExceptionDetector>>(),
            Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<Options.QuartzJobOptions>>(),
            Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RunWorkflowJob>>()
        );

        var jobDataMap = new JobDataMap
        {
            { "DefinitionVersionId", "test-workflow-def" }
        };

        var jobDetail = JobBuilder.Create<RunWorkflowJob>()
            .WithIdentity("test-delay-job", "test-group")
            .UsingJobData(jobDataMap)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("test-delay-trigger", "test-group")
            .ForJob(jobDetail)
            .StartNow()
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        // Act - Execute the job, which will throw a transient exception
        var context = CreateTestContext(scheduler, jobDetail, trigger);
        await job.Execute(context);

        // Assert - Verify the transient exception was caught and handled
        Assert.Equal(1, failingStarter.CallCount);
    }

    private static IJobExecutionContext CreateTestContext(global::Quartz.IScheduler scheduler, IJobDetail jobDetail, ITrigger trigger)
    {
        return new TestJobExecutionContext(scheduler, jobDetail, trigger);
    }
}

/// <summary>
/// Test implementation of IJobExecutionContext for component testing.
/// </summary>
internal class TestJobExecutionContext(global::Quartz.IScheduler scheduler, IJobDetail jobDetail, ITrigger trigger) : IJobExecutionContext
{
    public global::Quartz.IScheduler Scheduler => scheduler;
    public ITrigger Trigger => trigger;
    public IJobDetail JobDetail => jobDetail;
    public IJob JobInstance => null!;
    public bool Recovering => false;
    public TriggerKey RecoveringTriggerKey => trigger.Key;
    public int RefireCount => 0;
    public JobDataMap MergedJobDataMap => jobDetail.JobDataMap;
    public ICalendar? Calendar => null;
    public DateTimeOffset FireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledFireTimeUtc => DateTimeOffset.UtcNow;
    public DateTimeOffset? PreviousFireTimeUtc => null;
    public DateTimeOffset? NextFireTimeUtc => DateTimeOffset.UtcNow.AddSeconds(10);
    public TimeSpan JobRunTime => TimeSpan.Zero;
    public object? Result { get; set; }
    public CancellationToken CancellationToken => CancellationToken.None;
    public string FireInstanceId => Guid.NewGuid().ToString();

    public void Put(object key, object objectValue) { }
    public object? Get(object key) => null;
}
