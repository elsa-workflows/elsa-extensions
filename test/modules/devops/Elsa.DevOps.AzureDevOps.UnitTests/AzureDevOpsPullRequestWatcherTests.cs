using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers <see cref="AzureDevOpsPullRequestWatcher.CheckAsync"/>'s early-return path, taken when no organization URL
/// or token is configured. It must hand the caller's <c>knownFingerprint</c> back unchanged rather than substituting
/// an empty string for an unknown baseline: the seeding call from <c>InitializePullRequestWatch</c> passes
/// <c>knownFingerprint: null</c>, and collapsing that to <c>""</c> would make the next successful check compare a
/// real fingerprint against an empty one, find them different, and report an update that never happened. Also covers
/// the guard on the polling switches, which is what lets a running watcher wind itself down.
/// </summary>
public class AzureDevOpsPullRequestWatcherTests
{
    private static AzureDevOpsPullRequestWatcher CreateWatcher(AzureDevOpsPollingOptions options)
    {
        AzureDevOpsUserNameResolver userNameResolver = new(Substitute.For<IHttpContextAccessor>());
        AzureDevOpsTokenResolver tokenResolver = new(
            Options.Create(new AzureDevOpsOptions()),
            Substitute.For<IAzureDevOpsSecretReader>(),
            userNameResolver,
            NullLogger<AzureDevOpsTokenResolver>.Instance);
        AzureDevOpsPollingCredentialResolver credentialResolver = new(tokenResolver, Options.Create(options));
        AzureDevOpsWebhookEventHandler eventHandler = new(Substitute.For<IStimulusSender>(), NullLogger<AzureDevOpsWebhookEventHandler>.Instance);
        AzureDevOpsOrganizationUrlResolver organizationUrlResolver = new(Options.Create(new AzureDevOpsOptions()));

        return new AzureDevOpsPullRequestWatcher(
            new AzureDevOpsConnectionFactory(),
            credentialResolver,
            eventHandler,
            organizationUrlResolver,
            NullLogger<AzureDevOpsPullRequestWatcher>.Instance,
            Options.Create(options));
    }

    [Fact]
    public async Task CheckAsync_reports_an_unknown_baseline_as_unknown_when_no_organization_is_configured()
    {
        // Regression: substituting "" for a null baseline here means the very first check the watcher ever makes -
        // the seeding call from InitializePullRequestWatch, which always passes null - would store an empty string.
        // The next successful check would then compare a real fingerprint against "", find them different, and fire
        // git.pullrequest.updated for a pull request that never changed.
        AzureDevOpsPullRequestWatcher watcher = CreateWatcher(new AzureDevOpsPollingOptions { Enabled = true });

        PullRequestWatchResult result = await watcher.CheckAsync(
            new PullRequestWatchTarget("MyProject", "MyRepo", PullRequestId: 1),
            knownFingerprint: null,
            CancellationToken.None);

        Assert.False(result.Changed);
        Assert.True(result.Active);
        Assert.Null(result.Fingerprint);
    }

    [Fact]
    public async Task CheckAsync_leaves_a_known_baseline_untouched_when_no_organization_is_configured()
    {
        // Same early-return path, but with a real baseline already established: it must come back exactly as given,
        // not altered or collapsed, so the next check keeps comparing against what this one was told.
        AzureDevOpsPullRequestWatcher watcher = CreateWatcher(new AzureDevOpsPollingOptions { Enabled = true });

        PullRequestWatchResult result = await watcher.CheckAsync(
            new PullRequestWatchTarget("MyProject", "MyRepo", PullRequestId: 1),
            knownFingerprint: "existing-fingerprint",
            CancellationToken.None);

        Assert.False(result.Changed);
        Assert.True(result.Active);
        Assert.Equal("existing-fingerprint", result.Fingerprint);
    }

    [Theory]
    [MemberData(nameof(SwitchedOffOptions))]
    public async Task CheckAsync_winds_the_watcher_down_when_a_polling_switch_is_off(AzureDevOpsPollingOptions options)
    {
        // A watcher outlives the switch that created it: it is a timer-driven instance the scheduler resumes on its
        // own, so without this guard one instance per open pull request keeps calling Azure DevOps and dispatching
        // updates after polling has been switched off. Reporting the pull request as inactive is what sends the watch
        // activity down its Closed outcome and ends the instance.
        AzureDevOpsPullRequestWatcher watcher = CreateWatcher(options);

        PullRequestWatchResult result = await watcher.CheckAsync(
            new PullRequestWatchTarget("MyProject", "MyRepo", PullRequestId: 1),
            knownFingerprint: "existing-fingerprint",
            CancellationToken.None);

        Assert.False(result.Changed);
        Assert.False(result.Active);
        Assert.Equal("existing-fingerprint", result.Fingerprint);
    }

    [Fact]
    public async Task CheckAsync_keeps_a_watcher_alive_when_only_the_family_switch_is_off()
    {
        // PullRequests:Enabled governs what the host starts, not what a running instance may do - the same rule the
        // dispatchers follow, so that a poll someone asks for by hand is not silently ignored. A watcher therefore
        // keeps watching until the master switch or WatchUpdates goes off, or until its pull request closes.
        AzureDevOpsPollingOptions options = new() { Enabled = true };
        options.PullRequests.Enabled = false;
        AzureDevOpsPullRequestWatcher watcher = CreateWatcher(options);

        PullRequestWatchResult result = await watcher.CheckAsync(
            new PullRequestWatchTarget("MyProject", "MyRepo", PullRequestId: 1),
            knownFingerprint: "existing-fingerprint",
            CancellationToken.None);

        // Active, and stopped at the credential check rather than at a switch: nothing is configured to poll with.
        Assert.True(result.Active);
        Assert.False(result.Changed);
        Assert.Equal("existing-fingerprint", result.Fingerprint);
    }

    public static TheoryData<AzureDevOpsPollingOptions> SwitchedOffOptions()
    {
        AzureDevOpsPollingOptions watchingOff = new() { Enabled = true };
        watchingOff.PullRequests.WatchUpdates = false;

        return
        [
            new AzureDevOpsPollingOptions { Enabled = false },
            watchingOff,
        ];
    }
}
