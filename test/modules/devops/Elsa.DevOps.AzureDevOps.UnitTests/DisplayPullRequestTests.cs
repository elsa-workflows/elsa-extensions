using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Activities.PullRequests;
using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The contract a viewer reads: a bookmark naming the display target, and the pull request projection in the activity's
/// own state. Run through a real workflow, because none of that exists until Elsa has resolved the inputs and suspended
/// the run - the same arrangement as <see cref="DisplayWorkItemTests"/>.
/// </summary>
public class DisplayPullRequestTests
{
    private const string OrganizationUrl = "https://dev.azure.com/contoso";

    [Fact]
    public async Task SuspendsOnABookmarkNamingItsOwnDisplayTarget()
    {
        WorkflowState state = await RunAsync(NewPullRequest(51));

        Bookmark bookmark = Assert.Single(state.Bookmarks);
        DevOpsDisplayBookmark payload = Assert.IsType<DevOpsDisplayBookmark>(bookmark.Payload);

        // Kind is what a viewer guards on. The three displays share this payload type, so without it a build viewer
        // would claim a pull request bookmark: every member it reads is present on both.
        Assert.Equal(DisplayPullRequest.BookmarkKind, payload.Kind);
        Assert.Equal("azuredevops-pullrequest/v1", payload.Kind);

        // Asserted as a literal as well as against the constant, so renaming the constant cannot quietly rename the
        // wire value with it.
        Assert.Equal("PullRequest", payload.UiDisplayTarget);
        Assert.Equal(DisplayPullRequest.DisplayTargetName, payload.UiDisplayTarget);

        Assert.Equal("51", payload.ResourceId);
        Assert.False(string.IsNullOrWhiteSpace(payload.DisplayId));
    }

    [Fact]
    public async Task KeepsThePullRequestInItsOwnActivityStateSoAViewerCanReadIt()
    {
        WorkflowState state = await RunAsync(NewPullRequest(51));

        IDictionary<string, object> activityState = DisplayState(state);
        Bookmark bookmark = Assert.Single(state.Bookmarks);
        DevOpsDisplayBookmark payload = Assert.IsType<DevOpsDisplayBookmark>(bookmark.Payload);

        // The join between the two halves: a viewer holds a bookmark and has to find the state that belongs to it.
        Assert.Equal(payload.DisplayId, activityState[DisplayPullRequest.DisplayIdStateKey]);

        string json = Assert.IsType<string>(activityState[DisplayPullRequest.PullRequestStateKey]);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement snapshot = document.RootElement;

        Assert.Equal(51, snapshot.GetProperty("id").GetInt32());
        Assert.Equal("Point pull request polling at the right project", snapshot.GetProperty("title").GetString());
        Assert.Equal("Active", snapshot.GetProperty("status").GetString());
        Assert.Equal("Succeeded", snapshot.GetProperty("mergeStatus").GetString());
        Assert.False(snapshot.GetProperty("isDraft").GetBoolean());
        Assert.Equal("Alice Anderson", snapshot.GetProperty("createdBy").GetString());

        // Both branches without their refs/heads/ prefix: that prefix is a Git wire detail, and this card is read by
        // people deciding whether to open the pull request.
        Assert.Equal("feature/devops-displays", snapshot.GetProperty("sourceBranch").GetString());
        Assert.Equal("main", snapshot.GetProperty("targetBranch").GetString());

        // The project the link is built from comes off the repository, because a pull request read by id alone comes
        // back with the repository filled in and nothing else that names the project.
        Assert.Equal("Contoso Web Platform", snapshot.GetProperty("project").GetString());
        Assert.Equal(
            OrganizationUrl + "/Contoso%20Web%20Platform/_git/Contoso/pullrequest/51",
            snapshot.GetProperty("url").GetString());
    }

    [Fact]
    public async Task TranslatesReviewerVotesIntoWordsAReaderUnderstands()
    {
        GitPullRequest pullRequest = NewPullRequest(51);
        pullRequest.Reviewers =
        [
            new IdentityRefWithVote { DisplayName = "Approver", Vote = 10, IsRequired = true },
            new IdentityRefWithVote { DisplayName = "Blocker", Vote = -5 },
            new IdentityRefWithVote { DisplayName = "Silent" },
        ];

        WorkflowState state = await RunAsync(pullRequest);

        using JsonDocument document = JsonDocument.Parse((string)DisplayState(state)[DisplayPullRequest.PullRequestStateKey]);
        JsonElement reviewers = document.RootElement.GetProperty("reviewers");

        // The numbers are an Azure DevOps wire detail: a card rendering "-5" says nothing to whoever reads it, so the
        // translation happens here rather than being repeated in every viewer.
        Assert.Equal(3, reviewers.GetArrayLength());
        Assert.Equal("Approved", reviewers[0].GetProperty("vote").GetString());
        Assert.True(reviewers[0].GetProperty("isRequired").GetBoolean());
        Assert.Equal("Waiting for author", reviewers[1].GetProperty("vote").GetString());
        Assert.Equal("No vote", reviewers[2].GetProperty("vote").GetString());
    }

    [Fact]
    public async Task OffersNoLinkWhenThePullRequestDoesNotSayWhichRepositoryItIsIn()
    {
        GitPullRequest pullRequest = NewPullRequest(51);
        pullRequest.Repository = null;

        WorkflowState state = await RunAsync(pullRequest);

        using JsonDocument document = JsonDocument.Parse((string)DisplayState(state)[DisplayPullRequest.PullRequestStateKey]);

        // A pull request URL needs both the project and the repository. No link beats one that does not open, and the
        // viewer hides the button when this is absent.
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("url").ValueKind);
    }

    [Fact]
    public async Task CarriesTheHeadingTheWorkflowGaveIt()
    {
        WorkflowState state = await RunAsync(NewPullRequest(51), heading: "Deze PR wacht op jouw review");

        Assert.Equal("Deze PR wacht op jouw review", DisplayState(state)[DisplayPullRequest.HeadingStateKey]);
    }

    [Fact]
    public async Task LeavesNoHeadingWhenTheWorkflowNamedNone()
    {
        WorkflowState state = await RunAsync(NewPullRequest(51));

        // Absent rather than empty, so a viewer can tell "no heading was given" from "the heading is blank".
        Assert.False(DisplayState(state).ContainsKey(DisplayPullRequest.HeadingStateKey));
    }

    [Fact]
    public async Task CompletesOnceThePullRequestHasBeenSeen()
    {
        await using ServiceProvider provider = BuildProvider();

        Workflow workflow = Workflow.FromActivity(new DisplayPullRequest
        {
            PullRequest = new Input<GitPullRequest>(NewPullRequest(51)),
            OrganizationUrl = new Input<string>(OrganizationUrl),
        });

        WorkflowState suspended = await RunAsync(provider, workflow);
        WorkflowState resumed = await ResumeAsync(provider, workflow, suspended, suspended.Bookmarks.Single().Id);

        // Read-only means there is nothing to collect, so confirming is the whole answer - and it has to end the wait.
        Assert.Empty(resumed.Bookmarks);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
    }

    [Fact]
    public async Task RefusesAPullRequestWithoutAnId()
    {
        WorkflowState state = await RunAsync(new GitPullRequest());

        Assert.Equal(WorkflowSubStatus.Faulted, state.SubStatus);
        Assert.Contains(state.Incidents, incident => incident.Exception?.Message == "'PullRequest' must carry an id.");

        // And nothing was put on show: a bookmark here would be a viewer waiting on something it cannot open.
        Assert.Empty(state.Bookmarks);
    }

    [Fact]
    public async Task IsOfferedInTheDesignerUnderTheNameSavedDefinitionsBindTo()
    {
        await using ServiceProvider provider = BuildProvider();

        await provider.GetRequiredService<IActivityRegistryPopulator>()
            .PopulateRegistryAsync(CancellationToken.None);
        IActivityRegistry registry = provider.GetRequiredService<IActivityRegistry>();

        // {ActivityAttribute.Namespace}.{ClassName}: what a saved workflow definition refers to, and what the Studio
        // bookmark control matches a bookmark title on.
        Assert.NotNull(registry.Find(descriptor => descriptor.TypeName == "Elsa.AzureDevOps.PullRequests.DisplayPullRequest"));
    }

    [Fact]
    public void OffersThePullRequestTypeAsAVariableType()
    {
        using ServiceProvider provider = BuildProvider();

        ManagementOptions options = provider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        Assert.Contains(options.VariableDescriptors, descriptor => descriptor.Type == typeof(GitPullRequest));
    }

    private static GitPullRequest NewPullRequest(int id) =>
        new()
        {
            PullRequestId = id,
            Title = "Point pull request polling at the right project",
            Description = "De trigger indexeerde op DefaultProject.",
            Status = PullRequestStatus.Active,
            IsDraft = false,
            MergeStatus = PullRequestAsyncStatus.Succeeded,
            SourceRefName = "refs/heads/feature/devops-displays",
            TargetRefName = "refs/heads/main",
            CreationDate = new DateTime(2026, 8, 27, 8, 30, 0, DateTimeKind.Utc),
            CreatedBy = new IdentityRef { DisplayName = "Alice Anderson" },
            Repository = new GitRepository
            {
                Id = Guid.Parse("1f7d4e2c-9b3a-4f51-8c2d-0a6b5e7f1234"),
                Name = "Contoso",

                // A project whose name carries a space, because this organization has one and an unescaped link to it
                // is broken - see DevOpsDisplayText.Escape.
                ProjectReference = new TeamProjectReference { Name = "Contoso Web Platform" },
            },
            Reviewers = [],
        };

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa(elsa => elsa.UseAzureDevOps(feature =>
            feature.ConfigureOptions = options => options.DefaultOrganizationUrl = OrganizationUrl));

        return services.BuildServiceProvider();
    }

    private static async Task<WorkflowState> RunAsync(GitPullRequest pullRequest, string? heading = null)
    {
        await using ServiceProvider provider = BuildProvider();

        DisplayPullRequest activity = new()
        {
            PullRequest = new Input<GitPullRequest>(pullRequest),
            OrganizationUrl = new Input<string>(OrganizationUrl),
            Heading = heading == null ? null : new Input<string?>(heading),
        };

        return await RunAsync(provider, Workflow.FromActivity(activity));
    }

    private static async Task<WorkflowState> RunAsync(ServiceProvider provider, Workflow workflow)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();

        return (await runner.RunAsync(workflow, new RunWorkflowOptions())).WorkflowState;
    }

    private static async Task<WorkflowState> ResumeAsync(ServiceProvider provider, Workflow workflow, WorkflowState state, string bookmarkId)
    {
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IWorkflowRunner runner = scope.ServiceProvider.GetRequiredService<IWorkflowRunner>();

        return (await runner.RunAsync(workflow, state, new RunWorkflowOptions { BookmarkId = bookmarkId })).WorkflowState;
    }

    /// <summary>The activity state of the one display in this run.</summary>
    private static IDictionary<string, object> DisplayState(WorkflowState state) =>
        Assert.Single(state.ActivityExecutionContexts
            .Where(context => context.ActivityState?.ContainsKey(DisplayPullRequest.DisplayIdStateKey) == true)
            .Select(context => context.ActivityState!));
}
