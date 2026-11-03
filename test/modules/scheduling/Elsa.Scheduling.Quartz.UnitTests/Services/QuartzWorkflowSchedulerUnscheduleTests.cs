using Elsa.Common;
using Elsa.Common.Multitenancy;
using Elsa.Scheduling.Quartz.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Quartz;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

/// <summary>
/// Verifies that cleanup skips absent triggers while preserving tenant keys, cancellation and failures.
/// </summary>
public class QuartzWorkflowSchedulerUnscheduleTests
{
    [Theory]
    [InlineData(null, "Default")]
    [InlineData("tenant-a", "tenant-a")]
    [InlineData("tenant-b", "tenant-b")]
    public async Task UnscheduleAsync_WhenTriggerMissing_DoesNotMutateScheduler(string? tenantId, string group)
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        var sut = CreateScheduler(scheduler, out _, tenantId);
        var key = new TriggerKey("task-1", group);

        await sut.UnscheduleAsync(key.Name);

        scheduler.Verify(s => s.CheckExists(key, It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null, "Default")]
    [InlineData("tenant-a", "tenant-a")]
    [InlineData("tenant-b", "tenant-b")]
    public async Task UnscheduleAsync_WhenTriggerExists_UsesTenantKeyAndCallerToken(string? tenantId, string group)
    {
        using var cancellation = new CancellationTokenSource();
        var token = cancellation.Token;
        var key = new TriggerKey("task-1", group);
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(key, token)).ReturnsAsync(true);
        scheduler.Setup(s => s.UnscheduleJob(key, token)).ReturnsAsync(true);
        var sut = CreateScheduler(scheduler, out var factory, tenantId);

        await sut.UnscheduleAsync(key.Name, token);

        factory.Verify(f => f.GetScheduler(token), Times.Once);
        scheduler.Verify(s => s.CheckExists(key, token), Times.Once);
        scheduler.Verify(s => s.UnscheduleJob(key, token), Times.Once);
        scheduler.Verify(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnscheduleAsync_WhenTriggerDisappearsAfterCheck_CompletesWithoutRetryingRemoval()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        scheduler.Setup(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sut = CreateScheduler(scheduler, out _);

        await sut.UnscheduleAsync("task-1");

        scheduler.Verify(s => s.CheckExists(new TriggerKey("task-1", "Default"), It.IsAny<CancellationToken>()), Times.Once);
        scheduler.Verify(s => s.UnscheduleJob(new TriggerKey("task-1", "Default"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnscheduleAsync_WhenCleanupIsRepeated_RemovesTriggerOnlyOnce()
    {
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.SetupSequence(s => s.CheckExists(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        scheduler.Setup(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateScheduler(scheduler, out _);

        await sut.UnscheduleAsync("task-1");
        await sut.UnscheduleAsync("task-1");

        scheduler.Verify(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("factory")]
    [InlineData("check")]
    [InlineData("remove")]
    public async Task UnscheduleAsync_WhenCancelled_PropagatesCancellation(string stage)
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var token = cancellation.Token;
        var exception = new OperationCanceledException(token);
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<TriggerKey>(), token)).ReturnsAsync(true);
        var sut = CreateScheduler(scheduler, out var factory);

        if (stage == "factory")
            factory.Setup(f => f.GetScheduler(token)).ThrowsAsync(exception);
        else if (stage == "check")
            scheduler.Setup(s => s.CheckExists(It.IsAny<TriggerKey>(), token)).ThrowsAsync(exception);
        else
            scheduler.Setup(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), token)).ThrowsAsync(exception);

        var actual = await Assert.ThrowsAsync<OperationCanceledException>(() => sut.UnscheduleAsync("task-1", token).AsTask());

        Assert.Same(exception, actual);
        if (stage != "remove")
            scheduler.Verify(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("check")]
    [InlineData("remove")]
    public async Task UnscheduleAsync_WhenPersistenceFails_PropagatesFailure(string stage)
    {
        var exception = new JobPersistenceException("The Quartz store is unavailable.");
        var scheduler = new Mock<global::Quartz.IScheduler>();
        scheduler.Setup(s => s.CheckExists(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var sut = CreateScheduler(scheduler, out _);

        if (stage == "check")
            scheduler.Setup(s => s.CheckExists(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>())).ThrowsAsync(exception);
        else
            scheduler.Setup(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>())).ThrowsAsync(exception);

        var actual = await Assert.ThrowsAsync<JobPersistenceException>(() => sut.UnscheduleAsync("task-1").AsTask());

        Assert.Same(exception, actual);
        if (stage == "check")
            scheduler.Verify(s => s.UnscheduleJob(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static QuartzWorkflowScheduler CreateScheduler(Mock<global::Quartz.IScheduler> scheduler,
        out Mock<global::Quartz.ISchedulerFactory> factory, string? tenantId = null)
    {
        factory = new Mock<global::Quartz.ISchedulerFactory>();
        factory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(scheduler.Object);
        var tenantAccessor = new Mock<ITenantAccessor>();
        tenantAccessor.Setup(t => t.Tenant).Returns(tenantId == null ? null : new Tenant { Id = tenantId });

        return new QuartzWorkflowScheduler(factory.Object, Mock.Of<IJsonSerializer>(), tenantAccessor.Object,
            new JobKeyProvider(tenantAccessor.Object), NullLogger<QuartzWorkflowScheduler>.Instance);
    }
}
