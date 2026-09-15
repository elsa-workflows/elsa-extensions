using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers the keys one watcher is pinned to. Azure DevOps allocates pull request IDs per organization, so the number
/// alone identifies a pull request only as long as polling stays inside one organization — which a poll activity that
/// overrides the organization no longer does.
/// </summary>
public class AzureDevOpsPullRequestWatchStarterTests
{
    [Fact]
    public void GetInstanceId_keeps_the_shape_it_has_always_had_for_the_hosts_own_organization()
    {
        // Regression: watchers created before the organization entered the key are running in production right now,
        // pinned to this exact ID. Changing it for the normal case would leave those instances orphaned - still
        // timer-driven, still reporting updates - beside a fresh watcher reporting the same ones twice.
        Assert.Equal("AzureDevOpsPullRequestWatcher:1234", AzureDevOpsPullRequestWatchStarter.GetInstanceId(1234, organizationSegment: null));
    }

    [Fact]
    public void GetCorrelationId_keeps_the_shape_it_has_always_had_for_the_hosts_own_organization()
    {
        Assert.Equal("1234", AzureDevOpsPullRequestWatchStarter.GetCorrelationId(1234, organizationSegment: null));
    }

    [Fact]
    public void GetInstanceId_separates_the_same_pull_request_number_in_two_organizations()
    {
        // Without this the second organization's pull request finds the first one's watcher already running, and the
        // starter returns early with "already being watched" - so it is never watched at all.
        string first = AzureDevOpsPullRequestWatchStarter.GetInstanceId(1234, "dev.azure.com-contoso");
        string second = AzureDevOpsPullRequestWatchStarter.GetInstanceId(1234, "dev.azure.com-othercompany");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetCorrelationId_separates_the_same_pull_request_number_in_two_organizations()
    {
        // The workflow is a correlated singleton, so the correlation ID has to carry the organization as well; pinning
        // only the instance ID would still let the activation strategy refuse the second watcher.
        string first = AzureDevOpsPullRequestWatchStarter.GetCorrelationId(1234, "dev.azure.com-contoso");
        string second = AzureDevOpsPullRequestWatchStarter.GetCorrelationId(1234, "dev.azure.com-othercompany");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void GetInstanceId_names_the_organization_it_watches()
    {
        Assert.Equal(
            "AzureDevOpsPullRequestWatcher:dev.azure.com-othercompany:1234",
            AzureDevOpsPullRequestWatchStarter.GetInstanceId(1234, "dev.azure.com-othercompany"));
    }

    [Theory]
    [InlineData("https://dev.azure.com/contoso")]
    [InlineData("https://dev.azure.com/Contoso/")]
    [InlineData(null)]
    public void CanWatch_needs_no_secret_for_the_hosts_own_organization(string? organizationUrl)
    {
        // The configured polling credential is the right one here, so a watcher that falls back to it is correct - and
        // that is what every watcher started before the poll activities took inputs does.
        AzureDevOpsPullRequestWatchStarter starter = CreateStarter();

        Assert.True(starter.CanWatch(organizationUrl, tokenSecretName: null));
    }

    [Fact]
    public void CanWatch_refuses_another_organization_without_a_secret_to_resolve()
    {
        // A watcher started here would re-read a repository ID of another organization against the configured
        // credential, fail on it, and keep failing every interval for as long as the pull request stays open. The poll
        // still reports created and merged; only the watched update is given up.
        AzureDevOpsPullRequestWatchStarter starter = CreateStarter();

        Assert.False(starter.CanWatch("https://dev.azure.com/othercompany", tokenSecretName: null));
    }

    [Fact]
    public void CanWatch_accepts_another_organization_that_names_a_secret()
    {
        // A secret name is what a watcher can be handed: it resolves the token itself, so nothing sensitive has to be
        // stored on its instance.
        AzureDevOpsPullRequestWatchStarter starter = CreateStarter();

        Assert.True(starter.CanWatch("https://dev.azure.com/othercompany", "AzureDevOps:OtherPollingPat"));
    }

    private static AzureDevOpsPullRequestWatchStarter CreateStarter() =>
        new(
            Substitute.For<IWorkflowRuntime>(),
            Substitute.For<IWorkflowInstanceStore>(),
            Substitute.For<IWorkflowInstanceManager>(),
            new AzureDevOpsOrganizationUrlResolver(Options.Create(new AzureDevOpsOptions
            {
                DefaultOrganizationUrl = "https://dev.azure.com/contoso",
            })),
            NullLogger<AzureDevOpsPullRequestWatchStarter>.Instance);
}
