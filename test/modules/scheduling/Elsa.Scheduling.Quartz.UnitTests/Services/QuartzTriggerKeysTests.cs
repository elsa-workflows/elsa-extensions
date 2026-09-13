using Elsa.Scheduling.Quartz;
using Quartz;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

public class QuartzTriggerKeysTests
{
    [Fact]
    public void GetRetryTriggerKey_AppendsTheRetrySuffixInTheSameGroup()
    {
        var original = new TriggerKey("task-1", "tenant-a");

        var retry = QuartzTriggerKeys.GetRetryTriggerKey(original);

        Assert.Equal("task-1-retry", retry.Name);
        Assert.Equal("tenant-a", retry.Group);
    }

    [Fact]
    public void GetRetryTriggerKey_WhenAlreadyARetry_ReturnsTheSameKey()
    {
        var retry = new TriggerKey("task-1-retry", "Default");

        Assert.Equal(retry, QuartzTriggerKeys.GetRetryTriggerKey(retry));
        Assert.True(QuartzTriggerKeys.IsRetryTrigger(retry));
    }

    [Fact]
    public void IsRetryTrigger_OriginalKey_ReturnsFalse()
    {
        Assert.False(QuartzTriggerKeys.IsRetryTrigger(new TriggerKey("task-1", "Default")));
    }
}
