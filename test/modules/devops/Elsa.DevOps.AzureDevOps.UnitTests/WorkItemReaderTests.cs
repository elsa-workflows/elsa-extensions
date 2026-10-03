using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Models;
using Elsa.DevOps.AzureDevOps.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute.ExceptionExtensions;

namespace Elsa.DevOps.AzureDevOps.UnitTests;

/// <summary>
/// The read half of the work item tools, shared by the agent tool set and by the MCP tools of a host. Every test calls
/// it without an activity context, which is the shape an MCP call arrives in.
/// </summary>
public class WorkItemReaderTests
{
    [Fact]
    public async Task AsksWhichWorkItemIsMeantWhenNoIdIsGiven()
    {
        IWorkItemEditor editor = Substitute.For<IWorkItemEditor>();

        object result = await Reader(editor).GetAsync(null, null, CancellationToken.None);

        Assert.Equal(WorkItemReader.AskForTheWorkItem, result);
        Assert.Empty(editor.ReceivedCalls());
    }

    [Fact]
    public async Task ReadsTheWorkItemAzureDevOpsReturns()
    {
        IWorkItemEditor editor = Substitute.For<IWorkItemEditor>();
        WorkItemSnapshot snapshot = Snapshot(1234);
        editor.GetAsync(Arg.Any<WorkItemRequest>(), Arg.Any<CancellationToken>()).Returns(snapshot);

        object result = await Reader(editor).GetAsync(null, 1234, CancellationToken.None);

        Assert.Same(snapshot, result);
    }

    [Fact]
    public async Task HandsBackWhatAzureDevOpsRefused()
    {
        IWorkItemEditor editor = Substitute.For<IWorkItemEditor>();
        editor.GetAsync(Arg.Any<WorkItemRequest>(), Arg.Any<CancellationToken>())
            .Throws(new WorkItemEditorException("TF401232: Work item 999 does not exist"));

        object result = await Reader(editor).GetAsync(null, 999, CancellationToken.None);

        Assert.Contains("TF401232", Assert.IsType<string>(result), StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaysNothingWasQueriedWhenNoWiqlIsGiven()
    {
        IWorkItemEditor editor = Substitute.For<IWorkItemEditor>();

        object result = await Reader(editor).QueryAsync(null, "   ", null, CancellationToken.None);

        Assert.Contains("No wiql was given", Assert.IsType<string>(result), StringComparison.Ordinal);
        Assert.Empty(editor.ReceivedCalls());
    }

    [Fact]
    public async Task NamesTheProjectSettingWhenTheHostConfiguredNone()
    {
        IWorkItemEditor editor = Substitute.For<IWorkItemEditor>();

        object result = await Reader(editor, project: null)
            .SearchAsync(null, null, null, null, null, null, null, CancellationToken.None);

        Assert.Contains("AzureDevOps:DefaultProject", Assert.IsType<string>(result), StringComparison.Ordinal);
        Assert.Empty(editor.ReceivedCalls());
    }

    [Theory]
    [InlineData(null, 25)]
    [InlineData(0, 25)]
    [InlineData(5, 5)]
    [InlineData(999, 50)]
    public async Task BoundsHowManyRowsAreFetched(int? asked, int expected)
    {
        IWorkItemEditor editor = Substitute.For<IWorkItemEditor>();
        WorkItemQueryRequest? request = null;
        editor.SearchAsync(Arg.Any<WorkItemQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new WorkItemSearchResult([], false))
            .AndDoes(call => request = call.Arg<WorkItemQueryRequest>());

        await Reader(editor).QueryAsync(null, "SELECT [System.Id] FROM WorkItems", asked, CancellationToken.None);

        Assert.Equal(expected, request!.MaxResults);
    }

    [Fact]
    public async Task ComparesTheTagFilterWholeRatherThanAsASubstring()
    {
        IWorkItemEditor editor = Substitute.For<IWorkItemEditor>();
        editor.SearchAsync(Arg.Any<WorkItemQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new WorkItemSearchResult([Row(1, "problemId:19"), Row(2, "problemId:190")], false));

        object result = await Reader(editor)
            .SearchAsync(null, null, null, null, "problemId:19", null, null, CancellationToken.None);

        WorkItemSearchResult filtered = Assert.IsType<WorkItemSearchResult>(result);
        Assert.Equal([1], filtered.Items.Select(row => row.Id));
    }

    [Fact]
    public async Task LeavesAQueryResultAloneWhenNoTagWasAskedFor()
    {
        IWorkItemEditor editor = Substitute.For<IWorkItemEditor>();
        editor.SearchAsync(Arg.Any<WorkItemQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new WorkItemSearchResult([Row(1, "problemId:19"), Row(2, "problemId:190")], true));

        object result = await Reader(editor)
            .QueryAsync(null, "SELECT [System.Id] FROM WorkItems", null, CancellationToken.None);

        WorkItemSearchResult rows = Assert.IsType<WorkItemSearchResult>(result);
        Assert.Equal([1, 2], rows.Items.Select(row => row.Id));
        Assert.True(rows.MoreAvailable);
    }

    private static WorkItemRow Row(int id, string tags) =>
        new(id, "Bug", "Active", $"Work item {id}", tags, null, Url(id));

    private static WorkItemSnapshot Snapshot(int id) =>
        new(id, "Bug", "Active", $"Work item {id}", null, null, null, null, 1, Url(id));

    private static string Url(int id) => $"https://dev.azure.com/contoso/_workitems/edit/{id}";

    private static WorkItemReader Reader(IWorkItemEditor editor, string? project = "Contoso")
    {
        AzureDevOpsOptions options = new()
        {
            DefaultOrganizationUrl = "https://dev.azure.com/contoso",
            DefaultProject = project,
            DefaultToken = "pat-of-the-host",
        };

        WorkItemToolTargetResolver targets = new(
            new AzureDevOpsOrganizationUrlResolver(Options.Create(options)),
            new AzureDevOpsProjectResolver(Options.Create(options)),
            new AzureDevOpsTokenResolver(
                Options.Create(options),
                Substitute.For<IAzureDevOpsSecretReader>(),
                new AzureDevOpsUserNameResolver(Substitute.For<IHttpContextAccessor>()),
                NullLogger<AzureDevOpsTokenResolver>.Instance));

        return new WorkItemReader(editor, targets);
    }
}
