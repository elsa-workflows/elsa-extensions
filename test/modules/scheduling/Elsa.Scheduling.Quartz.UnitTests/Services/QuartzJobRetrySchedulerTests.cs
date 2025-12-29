using Elsa.Common;
using Elsa.Scheduling.Quartz.Services;
using Elsa.Scheduling.Quartz.UnitTests.Helpers;
using Moq;
using Quartz;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

public class QuartzJobRetrySchedulerTests
{
    [Fact]
    public async Task ScheduleRetryAsync_UsesSystemClockUtcNowPlusDelay()
    {
        var now = new DateTimeOffset(2025, 01, 02, 03, 04, 05, TimeSpan.Zero);
        var delay = TimeSpan.FromSeconds(10);
        var clock = new Mock<ISystemClock>();
        clock.SetupGet(x => x.UtcNow).Returns(now);

        ITrigger? capturedTrigger = null;
        var (context, scheduler) = QuartzJobTestHelper.CreateJobExecutionContext(new Dictionary<string, object>());
        scheduler
            .Setup(s => s.RescheduleJob(It.IsAny<TriggerKey>(), It.IsAny<ITrigger>(), It.IsAny<CancellationToken>()))
            .Callback<TriggerKey, ITrigger, CancellationToken>((_, t, _) => capturedTrigger = t)
            .ReturnsAsync(now);

        var options = QuartzJobTestHelper.CreateQuartzJobOptions(delay);
        var sut = new QuartzJobRetryScheduler(clock.Object, options.Object);

        await sut.ScheduleRetryAsync(context);

        Assert.NotNull(capturedTrigger);
        Assert.Equal(now.Add(delay), capturedTrigger!.StartTimeUtc);
    }
}
