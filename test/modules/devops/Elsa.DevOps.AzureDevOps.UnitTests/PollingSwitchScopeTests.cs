using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.Core;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Pins down which switch stops a poll and which one does not, for every family at once.
/// <c>AzureDevOps:Polling:Enabled</c> is the operator's kill switch: nothing polls while it is off, however the poll
/// was asked for. A family's own <c>Enabled</c> only decides whether that family's polling workflow is started at host
/// start, so a poll that is asked for anyway - the manual event on the built-in workflow, or a workflow of your own
/// carrying the poll activity - has to run.
/// </summary>
/// <remarks>
/// Every case is asserted at the credential check: no token is configured, so a poll that gets that far logs the
/// warning about it and stops there without reaching Azure DevOps. That warning is therefore the evidence that the
/// poll got past the switches, and its absence the evidence that it did not.
/// </remarks>
public class PollingSwitchScopeTests
{
    /// <summary>A poll of one family, ready to run, with the logger that reports how far it got.</summary>
    private sealed record Poll(string Family, Func<Task> Run, object Logger);

    /// <summary>
    /// An organization and a project are configured, so the only thing left to stop a poll that got past the switches
    /// is the credential it cannot resolve.
    /// </summary>
    private static AzureDevOpsOptions HostOptions() => new()
    {
        DefaultOrganizationUrl = "https://dev.azure.com/contoso",
        DefaultProject = "Contoso"
    };

    [Fact]
    public async Task DispatchAsync_still_runs_when_only_the_family_switch_is_off()
    {
        // The family switch governs the schedule, not the poll: it is read once at host start to decide whether to
        // start the family's polling workflow. Reading it here as well is what made a manual poll do nothing at all -
        // silently, because this path returns no events and logs nothing.
        foreach (Poll poll in EveryFamily(master: true, family: false))
        {
            await poll.Run();

            AssertWarningLogged(poll);
        }
    }

    [Fact]
    public async Task DispatchAsync_stops_at_the_master_switch_in_every_family()
    {
        // The operator's kill switch, and the one guarantee a workflow may not get around: no family reaches Azure
        // DevOps while this is off, whatever its own switch says and whoever asked for the poll.
        foreach (Poll poll in EveryFamily(master: false, family: true))
        {
            await poll.Run();

            AssertNoWarningLogged(poll);
        }
    }

    private static IEnumerable<Poll> EveryFamily(bool master, bool family) =>
        [WorkItems(master, family), Builds(master, family), PullRequests(master, family), Pushes(master, family)];

    private static Poll WorkItems(bool master, bool family)
    {
        AzureDevOpsPollingOptions options = WithSwitches(master, options => options.WorkItems.Enabled = family);
        ILogger<AzureDevOpsWorkItemPollingDispatcher> logger = Substitute.For<ILogger<AzureDevOpsWorkItemPollingDispatcher>>();
        AzureDevOpsWorkItemPollingDispatcher dispatcher = new(
            new AzureDevOpsConnectionFactory(),
            CredentialResolver(),
            EventHandler(),
            OrganizationUrlResolver(),
            ProjectResolver(),
            logger,
            Options.Create(options));

        return new Poll(
            "work item",
            () => dispatcher.DispatchAsync(options.WorkItems, DateTimeOffset.UtcNow.AddMinutes(-10), CancellationToken.None),
            logger);
    }

    private static Poll Builds(bool master, bool family)
    {
        AzureDevOpsPollingOptions options = WithSwitches(master, options => options.Builds.Enabled = family);
        ILogger<AzureDevOpsBuildPollingDispatcher> logger = Substitute.For<ILogger<AzureDevOpsBuildPollingDispatcher>>();
        AzureDevOpsBuildPollingDispatcher dispatcher = new(
            new AzureDevOpsConnectionFactory(),
            CredentialResolver(),
            EventHandler(),
            OrganizationUrlResolver(),
            ProjectResolver(),
            logger,
            Options.Create(options));

        return new Poll(
            "build",
            () => dispatcher.DispatchAsync(options.Builds, null, CancellationToken.None),
            logger);
    }

    private static Poll PullRequests(bool master, bool family)
    {
        AzureDevOpsPollingOptions options = WithSwitches(master, options => options.PullRequests.Enabled = family);
        ILogger<AzureDevOpsPullRequestPollingDispatcher> logger = Substitute.For<ILogger<AzureDevOpsPullRequestPollingDispatcher>>();
        AzureDevOpsPullRequestPollingDispatcher dispatcher = new(
            new AzureDevOpsConnectionFactory(),
            CredentialResolver(),
            EventHandler(),
            OrganizationUrlResolver(),
            ProjectResolver(),
            WatchStarter(),
            logger,
            Options.Create(options));

        return new Poll(
            "pull request",
            () => dispatcher.DispatchAsync(options.PullRequests, null, CancellationToken.None),
            logger);
    }

    private static Poll Pushes(bool master, bool family)
    {
        AzureDevOpsPollingOptions options = WithSwitches(master, options => options.Pushes.Enabled = family);
        ILogger<AzureDevOpsPushPollingDispatcher> logger = Substitute.For<ILogger<AzureDevOpsPushPollingDispatcher>>();
        AzureDevOpsPushPollingDispatcher dispatcher = new(
            new AzureDevOpsConnectionFactory(),
            CredentialResolver(),
            EventHandler(),
            OrganizationUrlResolver(),
            ProjectResolver(),
            logger,
            Options.Create(options));

        return new Poll(
            "push",
            () => dispatcher.DispatchAsync(options.Pushes, null, CancellationToken.None),
            logger);
    }

    private static AzureDevOpsPollingOptions WithSwitches(bool master, Action<AzureDevOpsPollingOptions> setFamilySwitch)
    {
        AzureDevOpsPollingOptions options = new() { Enabled = master };
        setFamilySwitch(options);

        return options;
    }

    private static AzureDevOpsPollingCredentialResolver CredentialResolver()
    {
        AzureDevOpsUserNameResolver userNameResolver = new(Substitute.For<IHttpContextAccessor>());
        AzureDevOpsTokenResolver tokenResolver = new(
            Options.Create(HostOptions()),
            Substitute.For<IAzureDevOpsSecretReader>(),
            userNameResolver,
            NullLogger<AzureDevOpsTokenResolver>.Instance);

        return new AzureDevOpsPollingCredentialResolver(tokenResolver, Options.Create(new AzureDevOpsPollingOptions()));
    }

    private static AzureDevOpsWebhookEventHandler EventHandler() =>
        new(Substitute.For<IStimulusSender>(), NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

    private static AzureDevOpsOrganizationUrlResolver OrganizationUrlResolver() =>
        new(Options.Create(HostOptions()));

    private static AzureDevOpsProjectResolver ProjectResolver() =>
        new(Options.Create(HostOptions()));

    private static AzureDevOpsPullRequestWatchStarter WatchStarter() =>
        new(
            Substitute.For<IWorkflowRuntime>(),
            Substitute.For<IWorkflowInstanceStore>(),
            Substitute.For<IWorkflowInstanceManager>(),
            OrganizationUrlResolver(),
            NullLogger<AzureDevOpsPullRequestWatchStarter>.Instance);

    /// <summary>
    /// Asserts the credential warning was logged. <c>LogWarning</c> is an extension method and so cannot be stubbed or
    /// asserted on directly; what the substitute actually records is the generic <see cref="ILogger.Log{TState}"/>
    /// underneath it, which is why this filters recorded calls by name and by the level in the first argument.
    /// </summary>
    private static void AssertWarningLogged(Poll poll) =>
        Assert.NotEmpty(WarningCalls(poll));

    private static void AssertNoWarningLogged(Poll poll) =>
        Assert.Empty(WarningCalls(poll));

    private static IEnumerable<ICall> WarningCalls(Poll poll) =>
        poll.Logger.ReceivedCalls()
            .Where(call =>
                call.GetMethodInfo().Name == nameof(ILogger.Log)
                && Equals(call.GetArguments()[0], LogLevel.Warning));
}
