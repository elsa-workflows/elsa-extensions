using System.Collections.Specialized;
using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;
using Quartz.Impl;

namespace Elsa.Scheduling.Quartz.ComponentTests;

/// <summary>
/// Exercises cleanup against a real Quartz scheduler, including repeating schedules and tenant isolation.
/// </summary>
public class QuartzWorkflowSchedulerUnscheduleTests
{
    /// <summary>
    /// Real Quartz cleanup removes one-shot, recurring and cron triggers without affecting matching names in other groups.
    /// </summary>
    [Theory]
    [InlineData("one-shot")]
    [InlineData("recurring")]
    [InlineData("cron")]
    public async Task UnscheduleAsync_RemovesOnlyTheCurrentTenantsTrigger(string scheduleType)
    {
        var factory = new StdSchedulerFactory(new NameValueCollection
        {
            ["quartz.scheduler.instanceName"] = $"unschedule-{Guid.NewGuid():N}"
        });
        var scheduler = await factory.GetScheduler();

        try
        {
            var currentTenant = CreateScheduler(factory, "tenant-a");
            var otherTenant = CreateScheduler(factory, "tenant-b");
            var defaultTenant = CreateScheduler(factory, null);
            const string taskName = "shared-task-name";

            await ScheduleAsync(currentTenant, taskName, scheduleType);
            await ScheduleAsync(otherTenant, taskName, scheduleType);
            await ScheduleAsync(defaultTenant, taskName, scheduleType);
            Assert.True(await scheduler.CheckExists(new TriggerKey(taskName, "tenant-a")));

            await currentTenant.UnscheduleAsync(taskName);
            await currentTenant.UnscheduleAsync(taskName);

            Assert.False(await scheduler.CheckExists(new TriggerKey(taskName, "tenant-a")));
            Assert.True(await scheduler.CheckExists(new TriggerKey(taskName, "tenant-b")));
            Assert.True(await scheduler.CheckExists(new TriggerKey(taskName, "Default")));
        }
        finally
        {
            await scheduler.Shutdown();
        }
    }

    /// <summary>
    /// Creates an adapter for one tenant while sharing the same real Quartz scheduler across groups.
    /// </summary>
    private static QuartzWorkflowScheduler CreateScheduler(ISchedulerFactory factory, string? tenantId)
    {
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.Tenant).Returns(tenantId == null ? null : new Tenant { Id = tenantId });

        return new QuartzWorkflowScheduler(factory, Mock.Of<IJsonSerializer>(), tenantAccessor.Object,
            new JobKeyProvider(tenantAccessor.Object), NullLogger<QuartzWorkflowScheduler>.Instance);
    }

    /// <summary>
    /// Creates a live trigger through the adapter using the requested schedule type without starting job execution.
    /// </summary>
    private static async Task ScheduleAsync(QuartzWorkflowScheduler scheduler, string taskName, string scheduleType)
    {
        var request = new global::Elsa.Scheduling.ScheduleNewWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionVersionId("definition-version-1"),
            TriggerActivityId = "activity-1"
        };
        var startAt = DateTimeOffset.UtcNow.AddHours(1);

        switch (scheduleType)
        {
            case "one-shot":
                await scheduler.ScheduleAtAsync(taskName, request, startAt);
                break;
            case "recurring":
                await scheduler.ScheduleRecurringAsync(taskName, request, startAt, TimeSpan.FromMinutes(1));
                break;
            case "cron":
                await scheduler.ScheduleCronAsync(taskName, request, "0 0/5 * * * ?");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scheduleType));
        }
    }
}
