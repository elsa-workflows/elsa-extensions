using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Activities.Builds;
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
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using AzureBuild = Microsoft.TeamFoundation.Build.WebApi.Build;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The contract a viewer reads: a bookmark naming the display target, and the build projection in the activity's own
/// state. Run through a real workflow, because none of that exists until Elsa has resolved the inputs and suspended
/// the run - the same arrangement as <see cref="DisplayWorkItemTests"/>.
/// </summary>
public class DisplayBuildTests
{
    private const string OrganizationUrl = "https://dev.azure.com/contoso";

    [Fact]
    public async Task SuspendsOnABookmarkNamingItsOwnDisplayTarget()
    {
        WorkflowState state = await RunAsync(NewBuild(4711));

        Bookmark bookmark = Assert.Single(state.Bookmarks);
        DevOpsDisplayBookmark payload = Assert.IsType<DevOpsDisplayBookmark>(bookmark.Payload);

        // Kind is what a viewer guards on. Without a discriminating value it claims every other feature's bookmark in
        // the host - and, since the three displays share this payload type, every other display's as well.
        Assert.Equal(DisplayBuild.BookmarkKind, payload.Kind);
        Assert.Equal("azuredevops-build/v1", payload.Kind);

        // The whole point of this activity: a display target of its own, spelled as a string, because the shared
        // UiDisplayTarget enum lives in a binary drop this repository cannot add a value to. Asserted as a literal as
        // well as against the constant, so renaming the constant cannot quietly rename the wire value with it.
        Assert.Equal("Build", payload.UiDisplayTarget);
        Assert.Equal(DisplayBuild.DisplayTargetName, payload.UiDisplayTarget);

        // Text, not a number: a repository is identified by a GUID, so the payload the three displays share carries
        // whatever names the thing on show.
        Assert.Equal("4711", payload.ResourceId);
        Assert.False(string.IsNullOrWhiteSpace(payload.DisplayId));
    }

    [Fact]
    public async Task KeepsTheBuildInItsOwnActivityStateSoAViewerCanReadIt()
    {
        WorkflowState state = await RunAsync(NewBuild(4711));

        IDictionary<string, object> activityState = DisplayState(state);
        Bookmark bookmark = Assert.Single(state.Bookmarks);
        DevOpsDisplayBookmark payload = Assert.IsType<DevOpsDisplayBookmark>(bookmark.Payload);

        // The join between the two halves. A viewer holds a bookmark and has to find the state that belongs to it;
        // matching on the display id is what makes that exact rather than "the only one in the instance".
        Assert.Equal(payload.DisplayId, activityState[DisplayBuild.DisplayIdStateKey]);

        // A string, deliberately: activity state goes through Elsa's own state serializer, and a string survives it
        // whatever that serializer decides about type discriminators or reference handling.
        string json = Assert.IsType<string>(activityState[DisplayBuild.BuildStateKey]);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement snapshot = document.RootElement;

        Assert.Equal(4711, snapshot.GetProperty("id").GetInt32());
        Assert.Equal("20260827.3", snapshot.GetProperty("buildNumber").GetString());
        Assert.Equal("Contoso.Web CI", snapshot.GetProperty("definition").GetString());
        Assert.Equal("Completed", snapshot.GetProperty("status").GetString());
        Assert.Equal("Succeeded", snapshot.GetProperty("result").GetString());
        Assert.Equal("Alice Anderson", snapshot.GetProperty("requestedFor").GetString());

        // Without its refs/heads/ prefix: that prefix is a Git wire detail, and the card is read by people.
        Assert.Equal("main", snapshot.GetProperty("sourceBranch").GetString());

        // The address a person opens, not the API address the build carries. The project name is escaped because this
        // organization has projects with spaces in their names.
        Assert.Equal(
            OrganizationUrl + "/Contoso%20Web%20Platform/_build/results?buildId=4711",
            snapshot.GetProperty("url").GetString());
    }

    [Fact]
    public async Task ReportsNoResultWhileTheBuildIsStillRunning()
    {
        AzureBuild build = NewBuild(4711);
        build.Status = BuildStatus.InProgress;
        build.Result = BuildResult.None;
        build.FinishTime = null;

        WorkflowState state = await RunAsync(build);

        using JsonDocument document = JsonDocument.Parse((string)DisplayState(state)[DisplayBuild.BuildStateKey]);

        // Null rather than "None". The enum's zero value means "no result yet", and a card saying a running build
        // resulted in None reads as a failure to whoever is looking at it.
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("result").ValueKind);
        Assert.Equal("InProgress", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task OffersNoLinkWhenTheBuildDoesNotSayWhichProjectItRanIn()
    {
        AzureBuild build = NewBuild(4711);
        build.Project = null;

        WorkflowState state = await RunAsync(build);

        using JsonDocument document = JsonDocument.Parse((string)DisplayState(state)[DisplayBuild.BuildStateKey]);

        // A build results URL is scoped to a project; an organization-level one is a 404. No link beats one that does
        // not open, and the viewer hides the button when this is absent.
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("url").ValueKind);
    }

    [Fact]
    public async Task CarriesTheHeadingTheWorkflowGaveIt()
    {
        WorkflowState state = await RunAsync(NewBuild(4711), heading: "Deze build gaat naar acceptatie");

        Assert.Equal("Deze build gaat naar acceptatie", DisplayState(state)[DisplayBuild.HeadingStateKey]);
    }

    [Fact]
    public async Task LeavesNoHeadingWhenTheWorkflowNamedNone()
    {
        WorkflowState state = await RunAsync(NewBuild(4711));

        // Absent rather than empty, so a viewer can tell "no heading was given" from "the heading is blank" and fall
        // back to one derived from the build itself.
        Assert.False(DisplayState(state).ContainsKey(DisplayBuild.HeadingStateKey));
    }

    [Fact]
    public async Task CompletesOnceTheBuildHasBeenSeen()
    {
        await using ServiceProvider provider = BuildProvider();

        Workflow workflow = Workflow.FromActivity(new DisplayBuild
        {
            Build = new Input<AzureBuild>(NewBuild(4711)),
            OrganizationUrl = new Input<string>(OrganizationUrl),
        });

        WorkflowState suspended = await RunAsync(provider, workflow);
        WorkflowState resumed = await ResumeAsync(provider, workflow, suspended, suspended.Bookmarks.Single().Id);

        // Read-only means there is nothing to collect, so confirming is the whole answer - and it has to end the wait,
        // or a workflow that shows a build never continues.
        Assert.Empty(resumed.Bookmarks);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
    }

    [Fact]
    public async Task RefusesABuildWithoutAnId()
    {
        WorkflowState state = await RunAsync(new AzureBuild());

        // Faulted with an incident, not a thrown exception: ActivityInputValidation.ThrowIfInvalid throws, and Elsa
        // turns that into an incident on the run. Refusing by returning false from CanExecuteAsync would instead skip
        // the activity silently and hang the container waiting for a completion signal that never comes.
        Assert.Equal(WorkflowSubStatus.Faulted, state.SubStatus);
        Assert.Contains(state.Incidents, incident => incident.Exception?.Message == "'Build' must carry an id.");

        // And nothing was put on show: a bookmark here would be a viewer waiting on a build that cannot be read.
        Assert.Empty(state.Bookmarks);
    }

    [Fact]
    public async Task IsOfferedInTheDesignerUnderTheNameSavedDefinitionsBindTo()
    {
        await using ServiceProvider provider = BuildProvider();

        await provider.GetRequiredService<IActivityRegistryPopulator>()
            .PopulateRegistryAsync(CancellationToken.None);
        IActivityRegistry registry = provider.GetRequiredService<IActivityRegistry>();

        // {ActivityAttribute.Namespace}.{ClassName}, which is what a saved workflow definition refers to and what the
        // Studio bookmark control matches a bookmark title on. Pinned here so a rename of either half fails a test
        // rather than quietly leaving every existing definition - and the Studio view - pointing at nothing.
        Assert.NotNull(registry.Find(descriptor => descriptor.TypeName == "Elsa.AzureDevOps.Builds.DisplayBuild"));
    }

    [Fact]
    public void OffersTheBuildTypeAsAVariableType()
    {
        using ServiceProvider provider = BuildProvider();

        ManagementOptions options = provider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        // The activity takes a Build as input, so a workflow author has to be able to keep one in a variable to bind
        // it. Elsa registers no Azure DevOps types itself; the feature's variable-type list is the only source.
        Assert.Contains(options.VariableDescriptors, descriptor => descriptor.Type == typeof(AzureBuild));
    }

    private static AzureBuild NewBuild(int id) =>
        new()
        {
            Id = id,
            BuildNumber = "20260827.3",
            Status = BuildStatus.Completed,
            Result = BuildResult.Succeeded,
            Reason = BuildReason.IndividualCI,
            SourceBranch = "refs/heads/main",
            SourceVersion = "9f1c2d4e",
            QueueTime = new DateTime(2026, 8, 27, 9, 0, 0, DateTimeKind.Utc),
            StartTime = new DateTime(2026, 8, 27, 9, 1, 0, DateTimeKind.Utc),
            FinishTime = new DateTime(2026, 8, 27, 9, 12, 0, DateTimeKind.Utc),
            Definition = new DefinitionReference { Name = "Contoso.Web CI" },

            // A project whose name carries a space, because this organization has one and an unescaped link to it is
            // broken - see DevOpsDisplayText.Escape.
            Project = new TeamProjectReference { Name = "Contoso Web Platform" },
            RequestedFor = new IdentityRef { DisplayName = "Alice Anderson" },
        };

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa(elsa => elsa.UseAzureDevOps(feature =>
            feature.ConfigureOptions = options => options.DefaultOrganizationUrl = OrganizationUrl));

        return services.BuildServiceProvider();
    }

    private static async Task<WorkflowState> RunAsync(AzureBuild build, string? heading = null)
    {
        await using ServiceProvider provider = BuildProvider();

        DisplayBuild activity = new()
        {
            Build = new Input<AzureBuild>(build),
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
            .Where(context => context.ActivityState?.ContainsKey(DisplayBuild.DisplayIdStateKey) == true)
            .Select(context => context.ActivityState!));
}
