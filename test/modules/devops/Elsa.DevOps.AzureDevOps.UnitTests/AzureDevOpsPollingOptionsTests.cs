using Elsa.DevOps.AzureDevOps.Configuration;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

public class AzureDevOpsPollingOptionsTests
{
    [Fact]
    public void ResolveInterval_prefers_the_interval_of_the_family()
    {
        var options = new AzureDevOpsPollingOptions { Interval = TimeSpan.FromMinutes(5) };
        options.Builds.Interval = TimeSpan.FromMinutes(1);

        Assert.Equal(TimeSpan.FromMinutes(1), options.ResolveInterval(options.Builds));
    }

    [Fact]
    public void ResolveInterval_falls_back_to_the_shared_interval_when_the_family_sets_none()
    {
        var options = new AzureDevOpsPollingOptions { Interval = TimeSpan.FromMinutes(7) };

        Assert.Equal(TimeSpan.FromMinutes(7), options.ResolveInterval(options.WorkItems));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ResolveInterval_falls_back_to_the_default_when_neither_interval_is_positive(int minutes)
    {
        // A non-positive interval would make the timer fire continuously, so it is treated as unconfigured.
        var options = new AzureDevOpsPollingOptions { Interval = TimeSpan.FromMinutes(minutes) };
        options.Pushes.Interval = TimeSpan.FromMinutes(minutes);

        Assert.Equal(AzureDevOpsPollingOptions.DefaultInterval, options.ResolveInterval(options.Pushes));
    }

    [Fact]
    public void Every_family_is_enabled_by_default_while_polling_itself_is_not()
    {
        // Polling:Enabled is the master switch; a family flag only narrows what an enabled poller does.
        var options = new AzureDevOpsPollingOptions();

        Assert.False(options.Enabled);
        Assert.True(options.WorkItems.Enabled);
        Assert.True(options.Builds.Enabled);
        Assert.True(options.PullRequests.Enabled);
        Assert.True(options.Pushes.Enabled);
    }

    [Fact]
    public void Work_item_polling_includes_deleted_items_by_default()
    {
        Assert.True(new AzureDevOpsPollingOptions().WorkItems.IncludeDeleted);
    }

    [Fact]
    public void Pull_request_polling_watches_updates_by_default()
    {
        Assert.True(new AzureDevOpsPollingOptions().PullRequests.WatchUpdates);
    }
}
