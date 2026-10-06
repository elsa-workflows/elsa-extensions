using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.DevOps.AzureDevOps.Events;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.DevOps.AzureDevOps.Triggers;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// A pull request trigger used to key on the project alone, so a workflow waiting for the pull request it had just
/// created was resumed by every other merge in the project - and by then the trigger had completed, which is not a
/// state a workflow can go back to waiting from. These cover both halves of the filter: the id a trigger indexes
/// itself with, and the id the handler reads out of a delivered event.
/// </summary>
public class PullRequestTriggerIdentityTests
{
    private const string Project = "Contoso";

    private static GitPullRequest PullRequest(int pullRequestId) => new() { PullRequestId = pullRequestId };

    [Fact]
    public async Task A_merged_delivery_is_offered_both_to_a_filtered_and_to_an_unfiltered_trigger()
    {
        // Both variants, because the filter is optional: a project-wide trigger has to keep firing, and the handler
        // cannot know which of the two is waiting.
        IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
        AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        await handler.HandleAsync(
            new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.PullRequestMerged, PullRequest(4711), Project),
            CancellationToken.None);

        await AssertSentAsync(stimulusSender, typeof(PullRequestMergedTrigger), AzureDevOpsWebhookEventTypes.PullRequestMerged, null);
        await AssertSentAsync(stimulusSender, typeof(PullRequestMergedTrigger), AzureDevOpsWebhookEventTypes.PullRequestMerged, "4711");
    }

    [Fact]
    public async Task An_updated_delivery_carries_the_pull_request_id_as_well()
    {
        IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
        AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        await handler.HandleAsync(
            new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.PullRequestUpdated, PullRequest(4711), Project),
            CancellationToken.None);

        await AssertSentAsync(stimulusSender, typeof(PullRequestUpdatedTrigger), AzureDevOpsWebhookEventTypes.PullRequestUpdated, "4711");
    }

    [Fact]
    public async Task Reads_the_pull_request_id_out_of_a_service_hook_resource()
    {
        // The typed payload above is the polling path. A Service Hook delivers camelCase JSON, and that is the path
        // that actually reaches a deployed workflow.
        IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
        AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        await handler.HandleAsync(
            new AzureDevOpsWebhookEvent(
                AzureDevOpsWebhookEventTypes.PullRequestMerged,
                Json("""{ "pullRequestId": 4711, "status": "completed", "title": "Iets" }"""),
                Project),
            CancellationToken.None);

        await AssertSentAsync(stimulusSender, typeof(PullRequestMergedTrigger), AzureDevOpsWebhookEventTypes.PullRequestMerged, "4711");
    }

    [Fact]
    public async Task A_created_delivery_is_not_keyed_on_a_pull_request_id()
    {
        // Nothing can be waiting for the creation of a pull request whose id it already knows, and an extra stimulus
        // per delivery that no trigger can be indexed with is pure noise in the log and in the store.
        IStimulusSender stimulusSender = Substitute.For<IStimulusSender>();
        AzureDevOpsWebhookEventHandler handler = new(stimulusSender, NullLogger<AzureDevOpsWebhookEventHandler>.Instance);

        await handler.HandleAsync(
            new AzureDevOpsWebhookEvent(AzureDevOpsWebhookEventTypes.PullRequestCreated, PullRequest(4711), Project),
            CancellationToken.None);

        await AssertSentAsync(stimulusSender, typeof(PullRequestCreatedTrigger), AzureDevOpsWebhookEventTypes.PullRequestCreated, null);
        await AssertNotSentAsync(stimulusSender, typeof(PullRequestCreatedTrigger), AzureDevOpsWebhookEventTypes.PullRequestCreated, "4711");
    }

    [Fact]
    public async Task A_trigger_told_which_pull_request_to_watch_indexes_itself_with_it()
    {
        AzureDevOpsWebhookBookmark bookmark = await BookmarkAsync(new PullRequestMergedTrigger
        {
            ProjectId = new Input<string>(Project),
            PullRequestId = new Input<int?>(4711),
        });

        Assert.Equal("4711", bookmark.PullRequestId);
        Assert.Equal(Project, bookmark.ProjectId);
    }

    [Fact]
    public async Task A_trigger_left_without_a_pull_request_id_keeps_listening_project_wide()
    {
        AzureDevOpsWebhookBookmark bookmark = await BookmarkAsync(new PullRequestMergedTrigger
        {
            ProjectId = new Input<string>(Project),
        });

        Assert.Null(bookmark.PullRequestId);
    }

    /// <summary>
    /// Pins the hash an unfiltered bookmark of this package gets, because that hash - not the record - is the wire
    /// format a suspended instance is matched on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Worth pinning rather than deriving, because the failure it guards against is silent. A stimulus whose hash
    /// matches no bookmark is not an error: the webhook still answers 202, nothing is logged, no incident is recorded,
    /// and every instance waiting on the old hash simply never resumes. Anything that changes the serialized text -
    /// adding a property to the record, renaming one, changing whether a null is written - changes the hash of every
    /// bookmark already stored, so this test failing means existing instances would be stranded and the stored hashes
    /// need migrating.
    /// </para>
    /// <para>
    /// Note what is and is not in the text below: <c>WorkItemId</c> and <c>WorkItemType</c> are written as nulls,
    /// while <c>PullRequestId</c> - carrying <c>[JsonIgnore(Condition = WhenWritingNull)]</c> - is absent. That is
    /// what lets an optional filter be added to this record without rehashing what came before it, and it is the only
    /// reason the pull request filter could be introduced at all.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task An_unfiltered_bookmark_hashes_as_it_did_before_the_filter_existed()
    {
        // The hashed JSON, spelled out rather than derived, so a change to the record shows up here as a diff.
        const string hashedJson =
            """{"EventType":"git.pullrequest.merged","ProjectId":"Contoso","WorkItemId":null,"WorkItemType":null}""";
        string expected = ExpectedHash(typeof(PullRequestMergedTrigger), hashedJson);

        Bookmark bookmark = await BookmarkRecordAsync(new PullRequestMergedTrigger
        {
            ProjectId = new Input<string>(Project),
        });

        Assert.Equal(expected, bookmark.Hash);
    }

    [Fact]
    public async Task A_filtered_bookmark_hashes_differently_from_an_unfiltered_one()
    {
        // The whole point of the filter: it has to change the hash, or a trigger watching one pull request would be
        // woken by every pull request in the project.
        Bookmark unfiltered = await BookmarkRecordAsync(new PullRequestMergedTrigger
        {
            ProjectId = new Input<string>(Project),
        });
        Bookmark filtered = await BookmarkRecordAsync(new PullRequestMergedTrigger
        {
            ProjectId = new Input<string>(Project),
            PullRequestId = new Input<int?>(4711),
        });

        Assert.Equal(
            ExpectedHash(
                typeof(PullRequestMergedTrigger),
                """{"EventType":"git.pullrequest.merged","ProjectId":"Contoso","WorkItemId":null,"WorkItemType":null,"PullRequestId":"4711"}"""),
            filtered.Hash);
        Assert.NotEqual(unfiltered.Hash, filtered.Hash);
    }

    /// <summary>
    /// The hash Elsa computes, rebuilt from first principles: <c>SHA256(activityTypeName + "|" + json)</c>, hex and
    /// uppercase. Rebuilt rather than taken from a constant so the JSON that goes into it stays readable in the test,
    /// which is the part that actually breaks. <c>ActivityInstanceId</c> is absent because these bookmarks are created
    /// with <c>includeActivityInstanceId: false</c>, and a null value drops out of the join.
    /// </summary>
    private static string ExpectedHash(Type triggerType, string hashedJson) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes($"{ActivityTypeNameHelper.GenerateTypeName(triggerType)}|{hashedJson}")));

    private static Task AssertSentAsync(IStimulusSender stimulusSender, Type triggerType, string eventType, string? pullRequestId) =>
        stimulusSender.Received(1).SendAsync(
            ActivityTypeNameHelper.GenerateTypeName(triggerType),
            // Positional rather than named, because a named argument out of position is not allowed here.
            new AzureDevOpsWebhookBookmark(eventType, Project, null, null, pullRequestId),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());

    private static Task AssertNotSentAsync(IStimulusSender stimulusSender, Type triggerType, string eventType, string? pullRequestId) =>
        stimulusSender.DidNotReceive().SendAsync(
            ActivityTypeNameHelper.GenerateTypeName(triggerType),
            // Positional rather than named, because a named argument out of position is not allowed here.
            new AzureDevOpsWebhookBookmark(eventType, Project, null, null, pullRequestId),
            Arg.Any<StimulusMetadata>(),
            Arg.Any<CancellationToken>());

    /// <summary>
    /// Runs the trigger with no event handed in, so it suspends and the bookmark it created is the only thing to
    /// inspect. That bookmark is what an arriving delivery is matched against, which makes it the half of the filter
    /// the handler tests above cannot see.
    /// </summary>
    private static async Task<AzureDevOpsWebhookBookmark> BookmarkAsync(IActivity trigger) =>
        Assert.IsType<AzureDevOpsWebhookBookmark>((await BookmarkRecordAsync(trigger)).Payload);

    /// <inheritdoc cref="BookmarkAsync"/>
    /// <remarks>
    /// Returns the bookmark record rather than its payload, because the hash is on the record and the hash is what an
    /// arriving delivery is matched on.
    /// </remarks>
    private static async Task<Bookmark> BookmarkRecordAsync(IActivity trigger)
    {
        ServiceCollection services = new();
        services.AddLogging();
        // Secrets are a feature the host opts into, so the extension never registers one itself.
        services.AddSingleton(Substitute.For<IAzureDevOpsSecretReader>());
        services.AddElsa(elsa => elsa.UseAzureDevOps());

        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();

        RunWorkflowResult result = await runner.RunAsync(trigger, new RunWorkflowOptions(), CancellationToken.None);
        return Assert.Single(result.WorkflowState.Bookmarks);
    }

    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();
}
