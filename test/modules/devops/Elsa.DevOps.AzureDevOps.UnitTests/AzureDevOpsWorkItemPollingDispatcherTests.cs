using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows.Runtime;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// Covers <see cref="AzureDevOpsWorkItemPollingDispatcher.DispatchDeletedAsync"/>'s early-return paths: every one of
/// them has to hand back the baseline it was given, untouched, rather than substituting an empty set for a baseline
/// it never established. A path that reads "no poll has run yet" as "the bin was empty" makes the very next poll
/// report the entire recycle bin as newly deleted.
/// </summary>
public class AzureDevOpsWorkItemPollingDispatcherTests
{
    private static AzureDevOpsWorkItemPollingDispatcher CreateDispatcher(AzureDevOpsPollingOptions options)
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
        AzureDevOpsProjectResolver projectResolver = new(Options.Create(new AzureDevOpsOptions()));

        return new AzureDevOpsWorkItemPollingDispatcher(
            new AzureDevOpsConnectionFactory(),
            credentialResolver,
            eventHandler,
            organizationUrlResolver,
            projectResolver,
            NullLogger<AzureDevOpsWorkItemPollingDispatcher>.Instance,
            Options.Create(options));
    }

    [Fact]
    public async Task DispatchDeletedAsync_leaves_an_unknown_baseline_unknown_while_polling_is_off()
    {
        // Regression: substituting [] for a null baseline here would tell the next poll "the bin was empty", and
        // every entry actually sitting in it would then be reported as newly deleted the moment polling is enabled.
        AzureDevOpsPollingOptions options = new() { Enabled = false };
        AzureDevOpsWorkItemPollingDispatcher dispatcher = CreateDispatcher(options);

        AzureDevOpsDeletedPollingResult result = await dispatcher.DispatchDeletedAsync(options.WorkItems, null, CancellationToken.None);

        Assert.Null(result.SeenIds);
        Assert.Empty(result.Events);
    }

    [Fact]
    public async Task DispatchDeletedAsync_leaves_an_unknown_baseline_unknown_while_deletions_are_switched_off()
    {
        // Same regression as above, via the family-specific switch rather than the master one: IncludeDeleted = false
        // must not be indistinguishable from "the bin was checked and found empty".
        AzureDevOpsPollingOptions options = new() { Enabled = true };
        options.WorkItems.IncludeDeleted = false;
        AzureDevOpsWorkItemPollingDispatcher dispatcher = CreateDispatcher(options);

        AzureDevOpsDeletedPollingResult result = await dispatcher.DispatchDeletedAsync(options.WorkItems, null, CancellationToken.None);

        Assert.Null(result.SeenIds);
        Assert.Empty(result.Events);
    }

    [Fact]
    public async Task DispatchDeletedAsync_leaves_an_existing_baseline_untouched_while_polling_is_off()
    {
        // Regression: an early return must hand back the caller's baseline as given, not collapse it to empty and
        // not otherwise alter it - the next poll has to keep comparing against exactly what this poll was told.
        AzureDevOpsPollingOptions options = new() { Enabled = false };
        AzureDevOpsWorkItemPollingDispatcher dispatcher = CreateDispatcher(options);

        AzureDevOpsDeletedPollingResult result = await dispatcher.DispatchDeletedAsync(options.WorkItems, [1, 2], CancellationToken.None);

        Assert.Equal([1, 2], result.SeenIds);
        Assert.Empty(result.Events);
    }
}
