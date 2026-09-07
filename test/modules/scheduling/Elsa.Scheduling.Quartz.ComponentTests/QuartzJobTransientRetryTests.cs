using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Resilience;
using Elsa.Scheduling.Quartz.ComponentTests.Abstractions;
using Elsa.Scheduling.Quartz.ComponentTests.Fixtures;
using Elsa.Scheduling.Quartz.ComponentTests.Helpers;
using Elsa.Scheduling.Quartz.Contracts;
using Elsa.Scheduling.Quartz.Jobs;
using Elsa.Scheduling.Quartz.Options;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;
using QuartzScheduler = Quartz.IScheduler;

namespace Elsa.Scheduling.Quartz.ComponentTests;

/// <summary>
/// Component tests for Quartz job transient retry behavior.
/// These tests validate that jobs properly retry on transient exceptions, count their attempts across reschedules,
/// stop once the configured maximum is reached, and give up immediately on non-transient exceptions.
/// </summary>
public class QuartzJobTransientRetryTests(SchedulingApp app) : AppComponentTest(app)
{
    private const string DefinitionVersionIdKey = "DefinitionVersionId";
    private const string DefinitionVersionId = "test-workflow-def";

    [Theory]
    [InlineData(2, typeof(TimeoutException), "Simulated transient timeout")]
    [InlineData(1, typeof(HttpRequestException), "Simulated network error")]
    public async Task RunWorkflowJob_TransientException_RetriesAndEventuallySucceeds(
        int failuresBeforeSuccess,
        Type exceptionType,
        string exceptionMessage)
    {
        var scenario = await CreateScenarioAsync(
            $"test-transient-{failuresBeforeSuccess}",
            failuresBeforeSuccess,
            (Exception)Activator.CreateInstance(exceptionType, exceptionMessage)!);

        // Every failing execution schedules the next retry and counts it on the rescheduled trigger.
        for (var attempt = 1; attempt <= failuresBeforeSuccess; attempt++)
        {
            await scenario.ExecuteAsync();

            Assert.Equal(attempt, scenario.Starter.CallCount);
            Assert.Equal(attempt, await scenario.GetRetryAttemptAsync());

            // The workflow inputs carried by the original trigger must survive the reschedule.
            Assert.Equal(DefinitionVersionId, (await scenario.GetTriggerAsync())!.JobDataMap.GetString(DefinitionVersionIdKey));
        }

        // The next execution succeeds, so no further retry is scheduled and the attempt count stops growing.
        await scenario.ExecuteAsync();

        Assert.Equal(failuresBeforeSuccess + 1, scenario.Starter.CallCount);
        Assert.Equal(failuresBeforeSuccess, await scenario.GetRetryAttemptAsync());
    }

    [Fact]
    public async Task RunWorkflowJob_TransientException_StopsAfterMaxRetryAttempts()
    {
        const int maxRetryAttempts = 2;
        var scenario = await CreateScenarioAsync(
            "test-exhaustion",
            failuresBeforeSuccess: int.MaxValue,
            new TimeoutException("Simulated permanent outage"),
            options => options.MaxRetryAttempts = maxRetryAttempts);

        for (var attempt = 1; attempt <= maxRetryAttempts; attempt++)
        {
            await scenario.ExecuteAsync();
            Assert.Equal(attempt, await scenario.GetRetryAttemptAsync());
        }

        // The retry budget is now exhausted: the job runs once more, but is not rescheduled again.
        await scenario.ExecuteAsync();

        Assert.Equal(maxRetryAttempts + 1, scenario.Starter.CallCount);
        Assert.Equal(maxRetryAttempts, await scenario.GetRetryAttemptAsync());

        // Exhausting the retries must not delete the job: that is reserved for non-transient failures.
        Assert.True(await scenario.Scheduler.CheckExists(scenario.Context.JobDetail.Key));
    }

    [Fact]
    public async Task RunWorkflowJob_RetriesDisabled_DoesNotReschedule()
    {
        var scenario = await CreateScenarioAsync(
            "test-retries-disabled",
            failuresBeforeSuccess: int.MaxValue,
            new TimeoutException("Simulated transient timeout"),
            options => options.RetryEnabled = false);

        await scenario.ExecuteAsync();

        Assert.Equal(1, scenario.Starter.CallCount);
        Assert.Equal(0, await scenario.GetRetryAttemptAsync());
        Assert.True(await scenario.Scheduler.CheckExists(scenario.Context.JobDetail.Key));
    }

    [Fact]
    public async Task RunWorkflowJob_NonTransientException_DoesNotRetry()
    {
        var scenario = await CreateScenarioAsync(
            "test-nontransient",
            failuresBeforeSuccess: int.MaxValue,
            new InvalidOperationException("Non-transient error"));

        await scenario.ExecuteAsync();

        // Assert - Job should be called once, then deleted (not retried)
        Assert.Equal(1, scenario.Starter.CallCount);
        Assert.Equal(0, await scenario.GetRetryAttemptAsync());
        Assert.False(await scenario.Scheduler.CheckExists(scenario.Context.JobDetail.Key));
    }

    /// <summary>
    /// Schedules a durable job with a trigger that carries the workflow payload, and wires a <see cref="RunWorkflowJob"/>
    /// around a workflow starter that fails the requested number of times. The trigger is scheduled far enough in the
    /// future for Quartz not to fire it on its own: the test drives execution explicitly.
    /// </summary>
    private async Task<RetryScenario> CreateScenarioAsync(
        string identifier,
        int failuresBeforeSuccess,
        Exception exception,
        Action<QuartzJobOptions>? configureOptions = null)
    {
        var scheduler = await WorkflowServer.GetSchedulerAsync();

        var starter = new FailingWorkflowStarter(Scope.ServiceProvider.GetRequiredService<IWorkflowStarter>())
        {
            FailuresBeforeSuccess = failuresBeforeSuccess,
            ExceptionToThrow = exception,
            SuccessResponse = new() { WorkflowInstanceId = $"{identifier}-instance" }
        };

        var jobDetail = JobBuilder.Create<RunWorkflowJob>()
            .WithIdentity($"{identifier}-job", "test-group")
            .StoreDurably()
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"{identifier}-trigger", "test-group")
            .ForJob(jobDetail)
            .UsingJobData(DefinitionVersionIdKey, DefinitionVersionId)
            .StartAt(DateTimeOffset.UtcNow.AddHours(1))
            .Build();

        await scheduler.ScheduleJob(jobDetail, trigger);

        var job = new RunWorkflowJob(
            Scope.ServiceProvider.GetRequiredService<ITenantAccessor>(),
            Scope.ServiceProvider.GetRequiredService<ITenantFinder>(),
            starter,
            CreateRetryScheduler(configureOptions),
            Scope.ServiceProvider.GetRequiredService<ILogger<RunWorkflowJob>>());

        return new(job, starter, new(scheduler, jobDetail, trigger), scheduler);
    }

    /// <summary>
    /// Creates a retry scheduler backed by the application's services, but with test-specific retry options. Delays are
    /// long enough that a scheduled retry never fires by itself during the test.
    /// </summary>
    private IQuartzJobRetryScheduler CreateRetryScheduler(Action<QuartzJobOptions>? configureOptions)
    {
        var options = new QuartzJobOptions
        {
            InitialRetryDelay = TimeSpan.FromSeconds(30),
            MaxRetryDelay = TimeSpan.FromMinutes(5),
            UseJitter = false
        };

        configureOptions?.Invoke(options);

        return new QuartzJobRetryScheduler(
            Scope.ServiceProvider.GetRequiredService<ISystemClock>(),
            Microsoft.Extensions.Options.Options.Create(options),
            Scope.ServiceProvider.GetRequiredService<IQuartzRetryDelayCalculator>(),
            Scope.ServiceProvider.GetRequiredService<ITransientExceptionDetector>(),
            Scope.ServiceProvider.GetRequiredService<ILogger<QuartzJobRetryScheduler>>());
    }

    private record RetryScenario(RunWorkflowJob Job, FailingWorkflowStarter Starter, TestJobExecutionContext Context, QuartzScheduler Scheduler)
    {
        /// <summary>
        /// Executes the job and then points the execution context at whatever trigger the scheduler now holds, the way
        /// Quartz would when it fires the rescheduled trigger.
        /// </summary>
        public async Task ExecuteAsync()
        {
            await Job.Execute(Context);
            var trigger = await GetTriggerAsync();

            if (trigger != null)
                Context.Trigger = trigger;
        }

        public async Task<ITrigger?> GetTriggerAsync() => await Scheduler.GetTrigger(Context.Trigger.Key);

        /// <summary>
        /// Gets the retry attempt persisted on the trigger currently in the scheduler, or 0 when no retry was scheduled.
        /// </summary>
        public async Task<int> GetRetryAttemptAsync()
        {
            var trigger = await GetTriggerAsync();

            if (trigger == null || !trigger.JobDataMap.TryGetString(QuartzJobDataKeys.RetryAttempt, out var attempt) || attempt == null)
                return 0;

            return int.Parse(attempt);
        }
    }
}

/// <summary>
/// Test implementation of IJobExecutionContext for component testing.
/// </summary>
internal class TestJobExecutionContext(QuartzScheduler scheduler, IJobDetail jobDetail, ITrigger trigger) : IJobExecutionContext
{
    public QuartzScheduler Scheduler => scheduler;

    /// <summary>
    /// The trigger that fired this execution. Settable so that a test can replay an execution with the trigger a
    /// previous retry produced.
    /// </summary>
    public ITrigger Trigger { get; set; } = trigger;

    public IJobDetail JobDetail => jobDetail;
    public IJob JobInstance => null!;
    public bool Recovering => false;
    public TriggerKey RecoveringTriggerKey => Trigger.Key;
    public int RefireCount => 0;

    public JobDataMap MergedJobDataMap
    {
        get
        {
            var map = new JobDataMap();
            map.PutAll(jobDetail.JobDataMap);
            map.PutAll(Trigger.JobDataMap);
            return map;
        }
    }

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
