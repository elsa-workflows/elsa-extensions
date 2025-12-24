using Elsa.Resilience;
using Elsa.Scheduling.Quartz.ComponentTests.Abstractions;
using Elsa.Scheduling.Quartz.ComponentTests.Fixtures;
using Elsa.Scheduling.Quartz.ComponentTests.Helpers;
using Elsa.Scheduling.Quartz.Contracts;
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
    [Theory]
    [InlineData(2, typeof(TimeoutException), "Simulated transient timeout")]
    [InlineData(1, typeof(HttpRequestException), "Simulated network error")]
    public async Task RunWorkflowJob_TransientException_RetriesAndEventuallySucceeds(
        int failuresBeforeSuccess,
        Type exceptionType,
        string exceptionMessage)
    {
        // Arrange - Create job manually with test double for controlled testing
        var (job, failingStarter, context) = await CreateTestJobSetupWithoutScheduling(
            failuresBeforeSuccess,
            (Exception)Activator.CreateInstance(exceptionType, exceptionMessage)!,
            $"test-transient-{failuresBeforeSuccess}"
        );

        // Act - Execute the job multiple times to simulate retries
        for (var i = 0; i < failuresBeforeSuccess; i++)
        {
            await job.Execute(context);
            Assert.Equal(i + 1, failingStarter.CallCount);
        }

        // Final attempt - should succeed
        await job.Execute(context);

        // Assert
        Assert.Equal(failuresBeforeSuccess + 1, failingStarter.CallCount);
    }

    [Fact]
    public async Task RunWorkflowJob_NonTransientException_DoesNotRetry()
    {
        // Arrange - Schedule job with Quartz so we can verify it gets deleted
        var (job, failingStarter, context) = await CreateTestJobSetupWithScheduling(
            10, // Would retry many times if transient
            new InvalidOperationException("Non-transient error"),
            "test-nontransient"
        );

        var scheduler = await WorkflowServer.GetSchedulerAsync();

        // Act
        await job.Execute(context);

        // Assert - Job should be called once, then deleted (not retried)
        Assert.Equal(1, failingStarter.CallCount);

        // Verify job was deleted from scheduler
        var jobExists = await scheduler.CheckExists(context.JobDetail.Key);
        Assert.False(jobExists);
    }

    /// <summary>
    /// Creates a test setup without scheduling the job with Quartz.
    /// Use when testing job execution logic directly without needing scheduler interaction.
    /// </summary>
    private async Task<(RunWorkflowJob job, FailingWorkflowStarter failingStarter, IJobExecutionContext context)>
        CreateTestJobSetupWithoutScheduling(int failuresBeforeSuccess, Exception exception, string jobIdentifier)
    {
        var scheduler = await WorkflowServer.GetSchedulerAsync();
        var workflowStarter = Scope.ServiceProvider.GetRequiredService<IWorkflowStarter>();

        var failingStarter = new FailingWorkflowStarter(workflowStarter)
        {
            FailuresBeforeSuccess = failuresBeforeSuccess,
            ExceptionToThrow = exception
        };

        var job = CreateRunWorkflowJob(failingStarter);
        var (jobDetail, trigger) = CreateJobDetailAndTrigger(jobIdentifier);
        
        // Don't schedule with Quartz - we're testing the Execute method directly
        var context = CreateTestContext(scheduler, jobDetail, trigger);

        return (job, failingStarter, context);
    }

    /// <summary>
    /// Creates a test setup and schedules the job with Quartz.
    /// Use when testing behavior that involves scheduler operations (e.g., job deletion).
    /// </summary>
    private async Task<(RunWorkflowJob job, FailingWorkflowStarter failingStarter, IJobExecutionContext context)>
        CreateTestJobSetupWithScheduling(int failuresBeforeSuccess, Exception exception, string jobIdentifier)
    {
        var scheduler = await WorkflowServer.GetSchedulerAsync();
        var workflowStarter = Scope.ServiceProvider.GetRequiredService<IWorkflowStarter>();

        var failingStarter = new FailingWorkflowStarter(workflowStarter)
        {
            FailuresBeforeSuccess = failuresBeforeSuccess,
            ExceptionToThrow = exception
        };

        var job = CreateRunWorkflowJob(failingStarter);
        var (jobDetail, trigger) = CreateJobDetailAndTrigger(jobIdentifier);

        // Schedule the job with Quartz so the job can interact with the scheduler
        // (e.g., delete itself on non-transient exceptions)
        await scheduler.ScheduleJob(jobDetail, trigger);
        var context = CreateTestContext(scheduler, jobDetail, trigger);

        return (job, failingStarter, context);
    }

    private RunWorkflowJob CreateRunWorkflowJob(IWorkflowStarter workflowStarter)
    {
        return new RunWorkflowJob(
            Scope.ServiceProvider.GetRequiredService<Common.Multitenancy.ITenantAccessor>(),
            Scope.ServiceProvider.GetRequiredService<Common.Multitenancy.ITenantFinder>(),
            workflowStarter,
            Scope.ServiceProvider.GetRequiredService<ITransientExceptionDetector>(),
            Scope.ServiceProvider.GetRequiredService<IQuartzJobRetryScheduler>(),
            Scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<RunWorkflowJob>>()
        );
    }

    private static (IJobDetail jobDetail, ITrigger trigger) CreateJobDetailAndTrigger(string identifier)
    {
        var jobDataMap = new JobDataMap
        {
            { "DefinitionVersionId", "test-workflow-def" }
        };

        var jobDetail = JobBuilder.Create<RunWorkflowJob>()
            .WithIdentity($"{identifier}-job", "test-group")
            .UsingJobData(jobDataMap)
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{identifier}-trigger", "test-group")
            .ForJob(jobDetail)
            .StartNow()
            .Build();

        return (jobDetail, trigger);
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
