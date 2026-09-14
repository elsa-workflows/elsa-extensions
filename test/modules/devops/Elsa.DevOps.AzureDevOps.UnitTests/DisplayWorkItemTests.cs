using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Activities.WorkItems;
using Elsa.DevOps.AzureDevOps.Bookmarks;
using Elsa.DevOps.AzureDevOps.Configuration;
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
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The contract a viewer reads: a bookmark naming the display target, and the work item projection in the activity's
/// own state. Run through a real workflow, because none of that exists until Elsa has resolved the inputs and
/// suspended the run.
/// </summary>
public class DisplayWorkItemTests
{
    private const string OrganizationUrl = "https://dev.azure.com/contoso";

    [Fact]
    public async Task SuspendsOnABookmarkNamingItsOwnDisplayTarget()
    {
        WorkflowState state = await RunAsync(NewWorkItem(41290));

        Bookmark bookmark = Assert.Single(state.Bookmarks);
        WorkItemDisplayBookmark payload = Assert.IsType<WorkItemDisplayBookmark>(bookmark.Payload);

        // Kind is what a viewer guards on. Without a discriminating value it claims every other feature's bookmark in
        // the host, because System.Text.Json fills absent members with defaults - the trap AgentChatBookmark documents.
        Assert.Equal(WorkItemDisplayBookmark.CurrentKind, payload.Kind);

        // The whole point of this activity: a display target of its own, spelled as a string, because the shared
        // UiDisplayTarget enum lives in a binary drop this repository cannot add a value to. Asserted as a literal as
        // well as against the constant, so renaming the constant cannot quietly rename the wire value with it.
        Assert.Equal("WorkItem", payload.UiDisplayTarget);
        Assert.Equal(WorkItemDisplayBookmark.DisplayTarget, payload.UiDisplayTarget);

        Assert.Equal(41290, payload.WorkItemId);
        Assert.False(string.IsNullOrWhiteSpace(payload.DisplayId));
    }

    [Fact]
    public async Task KeepsTheWorkItemInItsOwnActivityStateSoAViewerCanReadIt()
    {
        WorkflowState state = await RunAsync(NewWorkItem(41290));

        IDictionary<string, object> activityState = DisplayState(state);
        Bookmark bookmark = Assert.Single(state.Bookmarks);
        WorkItemDisplayBookmark payload = Assert.IsType<WorkItemDisplayBookmark>(bookmark.Payload);

        // The join between the two halves. A viewer holds a bookmark and has to find the state that belongs to it;
        // matching on the display id is what makes that exact rather than "the only one in the instance".
        Assert.Equal(payload.DisplayId, activityState[DisplayWorkItem.DisplayIdStateKey]);

        // A string, deliberately: activity state goes through Elsa's own state serializer, and a string survives it
        // whatever that serializer decides about type discriminators or reference handling.
        string json = Assert.IsType<string>(activityState[DisplayWorkItem.WorkItemStateKey]);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement snapshot = document.RootElement;

        Assert.Equal(41290, snapshot.GetProperty("id").GetInt32());
        Assert.Equal("Bug", snapshot.GetProperty("type").GetString());
        Assert.Equal("Active", snapshot.GetProperty("state").GetString());
        Assert.Equal("Taak blijft hangen", snapshot.GetProperty("title").GetString());
        Assert.Equal("Alice Anderson", snapshot.GetProperty("assignedTo").GetString());

        // The address a person opens, not the API address the work item carries: the link is half of what this view is
        // for, and the _apis/wit/workItems address opens JSON.
        Assert.Equal(OrganizationUrl + "/_workitems/edit/41290", snapshot.GetProperty("url").GetString());
    }

    [Fact]
    public async Task CarriesTheHeadingTheWorkflowGaveIt()
    {
        WorkflowState state = await RunAsync(NewWorkItem(41290), heading: "Deze bug gaat mee in de release");

        Assert.Equal("Deze bug gaat mee in de release", DisplayState(state)[DisplayWorkItem.HeadingStateKey]);
    }

    [Fact]
    public async Task LeavesNoHeadingWhenTheWorkflowNamedNone()
    {
        WorkflowState state = await RunAsync(NewWorkItem(41290));

        // Absent rather than empty, so a viewer can tell "no heading was given" from "the heading is blank" and fall
        // back to one derived from the work item itself.
        Assert.False(DisplayState(state).ContainsKey(DisplayWorkItem.HeadingStateKey));
    }

    [Fact]
    public async Task CompletesOnceTheWorkItemHasBeenSeen()
    {
        await using ServiceProvider provider = BuildProvider();

        Workflow workflow = Workflow.FromActivity(new DisplayWorkItem
        {
            WorkItem = new Input<WorkItem>(NewWorkItem(41290)),
            OrganizationUrl = new Input<string>(OrganizationUrl),
        });

        WorkflowState suspended = await RunAsync(provider, workflow);
        WorkflowState resumed = await ResumeAsync(provider, workflow, suspended, suspended.Bookmarks.Single().Id);

        // Read-only means there is nothing to collect, so confirming is the whole answer - and it has to end the wait,
        // or a workflow that shows a work item never continues.
        Assert.Empty(resumed.Bookmarks);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
    }

    [Fact]
    public async Task RefusesAWorkItemWithoutAnId()
    {
        // The shape a Service Hook payload arrives in when its casing did not bind - the failure
        // WorkItemCommentedTriggerTests pins down. A link to work item 0 is a view that looks like it worked, so the
        // run faults instead.
        WorkflowState state = await RunAsync(new WorkItem { Fields = new Dictionary<string, object>() });

        // Faulted with an incident, not a thrown exception: ActivityInputValidation.ThrowIfInvalid throws, and Elsa
        // turns that into an incident on the run. Refusing by returning false from CanExecuteAsync would instead skip
        // the activity silently and hang the container waiting for a completion signal that never comes.
        Assert.Equal(WorkflowSubStatus.Faulted, state.SubStatus);
        Assert.Contains(state.Incidents, incident => incident.Exception?.Message == "'WorkItem' must carry an id.");

        // And nothing was put on show: a bookmark here would be a viewer waiting on a work item that cannot be read.
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
        Assert.NotNull(registry.Find(descriptor => descriptor.TypeName == "Elsa.AzureDevOps.WorkItems.DisplayWorkItem"));
    }

    [Fact]
    public void OffersTheWorkItemTypeAsAVariableType()
    {
        using ServiceProvider provider = BuildProvider();

        ManagementOptions options = provider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        // The activity takes a WorkItem as input, so a workflow author has to be able to keep one in a variable to bind
        // it. Elsa registers no Azure DevOps types itself; the feature's variable-type list is the only source.
        Assert.Contains(options.VariableDescriptors, descriptor => descriptor.Type == typeof(WorkItem));
    }

    private static WorkItem NewWorkItem(int id) =>
        new()
        {
            Id = id,
            Rev = 7,
            Fields = new Dictionary<string, object>
            {
                ["System.WorkItemType"] = "Bug",
                ["System.State"] = "Active",
                ["System.Title"] = "Taak blijft hangen",
                ["System.AssignedTo"] = "Alice Anderson",
                ["System.Tags"] = "workflow; studio",
                ["System.ChangedDate"] = new DateTime(2026, 8, 26, 9, 0, 0, DateTimeKind.Utc),
            },
        };

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa(elsa => elsa.UseAzureDevOps(feature =>
            feature.ConfigureOptions = options => options.DefaultOrganizationUrl = OrganizationUrl));

        return services.BuildServiceProvider();
    }

    private static async Task<WorkflowState> RunAsync(WorkItem workItem, string? heading = null)
    {
        await using ServiceProvider provider = BuildProvider();

        DisplayWorkItem activity = new()
        {
            WorkItem = new Input<WorkItem>(workItem),
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
            .Where(context => context.ActivityState?.ContainsKey(DisplayWorkItem.DisplayIdStateKey) == true)
            .Select(context => context.ActivityState!));
}
