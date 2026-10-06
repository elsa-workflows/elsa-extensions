using System.Text.Json;
using Elsa.DevOps.AzureDevOps.Activities.Repositories;
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

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The contract a viewer reads: a bookmark naming the display target, and the repository projection in the activity's
/// own state. Run through a real workflow, because none of that exists until Elsa has resolved the inputs and suspended
/// the run - the same arrangement as <see cref="DisplayWorkItemTests"/>.
/// </summary>
public class DisplayRepositoryTests
{
    private const string OrganizationUrl = "https://dev.azure.com/contoso";
    private static readonly Guid RepositoryId = Guid.Parse("1f7d4e2c-9b3a-4f51-8c2d-0a6b5e7f1234");

    [Fact]
    public async Task SuspendsOnABookmarkNamingItsOwnDisplayTarget()
    {
        WorkflowState state = await RunAsync(NewRepository());

        Bookmark bookmark = Assert.Single(state.Bookmarks);
        DevOpsDisplayBookmark payload = Assert.IsType<DevOpsDisplayBookmark>(bookmark.Payload);

        Assert.Equal(DisplayRepository.BookmarkKind, payload.Kind);
        Assert.Equal("azuredevops-repository/v1", payload.Kind);

        // Asserted as a literal as well as against the constant, so renaming the constant cannot quietly rename the
        // wire value with it.
        Assert.Equal("Repository", payload.UiDisplayTarget);
        Assert.Equal(DisplayRepository.DisplayTargetName, payload.UiDisplayTarget);

        // The one display whose resource id is not a number, and the reason the shared payload carries text: a
        // repository is identified by a GUID.
        Assert.Equal(RepositoryId.ToString(), payload.ResourceId);
        Assert.False(string.IsNullOrWhiteSpace(payload.DisplayId));
    }

    [Fact]
    public async Task KeepsTheRepositoryInItsOwnActivityStateSoAViewerCanReadIt()
    {
        WorkflowState state = await RunAsync(NewRepository());

        IDictionary<string, object> activityState = DisplayState(state);
        Bookmark bookmark = Assert.Single(state.Bookmarks);
        DevOpsDisplayBookmark payload = Assert.IsType<DevOpsDisplayBookmark>(bookmark.Payload);

        // The join between the two halves: a viewer holds a bookmark and has to find the state that belongs to it.
        Assert.Equal(payload.DisplayId, activityState[DisplayRepository.DisplayIdStateKey]);

        string json = Assert.IsType<string>(activityState[DisplayRepository.RepositoryStateKey]);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement snapshot = document.RootElement;

        Assert.Equal(RepositoryId.ToString(), snapshot.GetProperty("id").GetString());
        Assert.Equal("Contoso", snapshot.GetProperty("name").GetString());
        Assert.Equal("Contoso Web Platform", snapshot.GetProperty("project").GetString());

        // Without its refs/heads/ prefix, like every other branch this feature shows.
        Assert.Equal("main", snapshot.GetProperty("defaultBranch").GetString());

        // The address the API itself reported. A repository read is the one call that answers with a browser URL of
        // its own, and it is right by construction where a derived one is right by assumption.
        Assert.Equal("https://dev.azure.com/contoso/Contoso%20Web%20Platform/_git/Contoso", snapshot.GetProperty("url").GetString());
    }

    [Fact]
    public async Task DerivesTheLinkWhenTheApiReportedNoWebAddress()
    {
        GitRepository repository = NewRepository();
        repository.WebUrl = null;

        WorkflowState state = await RunAsync(repository);

        using JsonDocument document = JsonDocument.Parse((string)DisplayState(state)[DisplayRepository.RepositoryStateKey]);

        // The fallback, escaped: this organization has projects with spaces in their names.
        Assert.Equal(
            OrganizationUrl + "/Contoso%20Web%20Platform/_git/Contoso",
            document.RootElement.GetProperty("url").GetString());
    }

    [Fact]
    public async Task CarriesTheHeadingTheWorkflowGaveIt()
    {
        WorkflowState state = await RunAsync(NewRepository(), heading: "Deze repository wordt gearchiveerd");

        Assert.Equal("Deze repository wordt gearchiveerd", DisplayState(state)[DisplayRepository.HeadingStateKey]);
    }

    [Fact]
    public async Task LeavesNoHeadingWhenTheWorkflowNamedNone()
    {
        WorkflowState state = await RunAsync(NewRepository());

        // Absent rather than empty, so a viewer can tell "no heading was given" from "the heading is blank".
        Assert.False(DisplayState(state).ContainsKey(DisplayRepository.HeadingStateKey));
    }

    [Fact]
    public async Task CompletesOnceTheRepositoryHasBeenSeen()
    {
        await using ServiceProvider provider = BuildProvider();

        Workflow workflow = Workflow.FromActivity(new DisplayRepository
        {
            Repository = new Input<GitRepository>(NewRepository()),
            OrganizationUrl = new Input<string>(OrganizationUrl),
        });

        WorkflowState suspended = await RunAsync(provider, workflow);
        WorkflowState resumed = await ResumeAsync(provider, workflow, suspended, suspended.Bookmarks.Single().Id);

        // Read-only means there is nothing to collect, so confirming is the whole answer - and it has to end the wait.
        Assert.Empty(resumed.Bookmarks);
        Assert.Equal(WorkflowStatus.Finished, resumed.Status);
    }

    [Fact]
    public async Task RefusesARepositoryWithoutAnId()
    {
        WorkflowState state = await RunAsync(new GitRepository { Name = "Contoso" });

        // The empty GUID is what "no id" looks like for a repository, and it is refused for the same reason a build
        // without an id is: a card with a name and no way to open it looks like it worked.
        Assert.Equal(WorkflowSubStatus.Faulted, state.SubStatus);
        Assert.Contains(state.Incidents, incident => incident.Exception?.Message == "'Repository' must carry an id.");
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
        Assert.NotNull(registry.Find(descriptor => descriptor.TypeName == "Elsa.AzureDevOps.Repositories.DisplayRepository"));
    }

    [Fact]
    public void OffersTheRepositoryTypeAsAVariableType()
    {
        using ServiceProvider provider = BuildProvider();

        ManagementOptions options = provider.GetRequiredService<IOptions<ManagementOptions>>().Value;

        Assert.Contains(options.VariableDescriptors, descriptor => descriptor.Type == typeof(GitRepository));
    }

    private static GitRepository NewRepository() =>
        new()
        {
            Id = RepositoryId,
            Name = "Contoso",
            DefaultBranch = "refs/heads/main",
            Size = 12_582_912,
            RemoteUrl = "https://dev.azure.com/contoso/Contoso%20Web%20Platform/_git/Contoso",
            WebUrl = "https://dev.azure.com/contoso/Contoso%20Web%20Platform/_git/Contoso",
            ProjectReference = new TeamProjectReference { Name = "Contoso Web Platform" },
        };

    private static ServiceProvider BuildProvider()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddElsa(elsa => elsa.UseAzureDevOps(feature =>
            feature.ConfigureOptions = options => options.DefaultOrganizationUrl = OrganizationUrl));

        return services.BuildServiceProvider();
    }

    private static async Task<WorkflowState> RunAsync(GitRepository repository, string? heading = null)
    {
        await using ServiceProvider provider = BuildProvider();

        DisplayRepository activity = new()
        {
            Repository = new Input<GitRepository>(repository),
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
            .Where(context => context.ActivityState?.ContainsKey(DisplayRepository.DisplayIdStateKey) == true)
            .Select(context => context.ActivityState!));
}
