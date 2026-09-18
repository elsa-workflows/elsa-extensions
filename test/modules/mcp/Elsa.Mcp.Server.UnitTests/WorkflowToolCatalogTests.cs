using System.Text.Json;
using Elsa.Mcp.Server.Configuration;
using Elsa.Mcp.Server.Services;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Elsa.Mcp.Server.UnitTests;

public class WorkflowToolCatalogTests
{
    [Fact]
    public void ToTool_DescribesACollectionInputAsAnArrayWithTypedItems()
    {
        // VS Code refuses the whole tool when an array property has no items schema, so the element type has to be
        // spelled out.
        JsonElement schema = BuildSchemaFor(typeof(ICollection<string>));

        Assert.Equal("array", schema.GetProperty("type").GetString());
        Assert.Equal("string", schema.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void ToTool_DescribesAnArrayInputAsAnArrayWithTypedItems()
    {
        JsonElement schema = BuildSchemaFor(typeof(int[]));

        Assert.Equal("array", schema.GetProperty("type").GetString());
        Assert.Equal("integer", schema.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void ToTool_DescribesANestedCollectionInputWithNestedItems()
    {
        JsonElement schema = BuildSchemaFor(typeof(ICollection<ICollection<string>>));

        Assert.Equal("array", schema.GetProperty("type").GetString());
        JsonElement items = schema.GetProperty("items");
        Assert.Equal("array", items.GetProperty("type").GetString());
        Assert.Equal("string", items.GetProperty("items").GetProperty("type").GetString());
    }

    [Fact]
    public void ToTool_DescribesANonGenericEnumerableInputAsAnArrayWithUnconstrainedItems()
    {
        // The element type of a non-generic enumerable is unknowable, but the items schema still has to be there —
        // an empty schema honestly says "anything" without failing the client's validation.
        JsonElement schema = BuildSchemaFor(typeof(System.Collections.ArrayList));

        Assert.Equal("array", schema.GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Object, schema.GetProperty("items").ValueKind);
    }

    [Fact]
    public void ToTool_DescribesADictionaryInputAsAnObjectWithoutItems()
    {
        // Dictionaries are enumerable too, but they describe a map: no array, no items.
        JsonElement schema = BuildSchemaFor(typeof(IDictionary<string, object?>));

        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.False(schema.TryGetProperty("items", out _));
    }

    // The names of the workflows this server actually exposes. The first two are the case that prompted the change:
    // both are drawn in Studio, so both were named after a generated hex string, and a request to start a PR review
    // started the work item assistant instead — no error, the wrong workflow simply ran.
    [Theory]
    [InlineData("PR - Review task", "49ba274a987b98d4", "pr-review-task")]
    [InlineData("Workitem Assistant Starter", "47fe1d141a7f4694", "workitem-assistant-starter")]
    [InlineData("Add workitem comment", "2b199394a63d1b52", "add-workitem-comment")]
    [InlineData("Start agent on work item", "StartAgentForWorkItemWorkflow", "start-agent-on-work-item")]
    [InlineData("List running versions", "DeploymentStatus", "list-running-versions")]
    public void ToTool_NamesTheToolAfterTheWorkflow(string workflowName, string definitionId, string expected)
    {
        Tool tool = BuildTool(new WorkflowDefinition { DefinitionId = definitionId, Name = workflowName });

        Assert.Equal(expected, tool.Name);
    }

    [Fact]
    public void ToTool_ReducesAWorkflowNameToWhatEveryClientAccepts()
    {
        // A tool name is constrained to ^[a-zA-Z0-9_-]{1,64}$, so anything else has to go — accents folded rather than
        // dropped, because "één" turning into "n" would name the tool after the wrong word.
        Tool tool = BuildTool(new WorkflowDefinition { DefinitionId = "abc123", Name = "Zorg & Welzijn: één (v2)" });

        Assert.Equal("zorg-welzijn-een-v2", tool.Name);
    }

    [Fact]
    public void ToTool_FallsBackToTheDefinitionIdWhenTheNameReducesToNothing()
    {
        Tool tool = BuildTool(new WorkflowDefinition { DefinitionId = "49ba274a987b98d4", Name = "→ ✓" });

        Assert.Equal("49ba274a987b98d4", tool.Name);
    }

    [Fact]
    public void ToTool_KeepsTheToolNameWithinTheClientLimit()
    {
        Tool tool = BuildTool(new WorkflowDefinition { DefinitionId = "abc123", Name = new string('a', 40) + " " + new string('b', 40) });

        Assert.Equal(64, tool.Name.Length);
        Assert.DoesNotContain("--", tool.Name);
        Assert.False(tool.Name.EndsWith('-'));
    }

    [Fact]
    public void ToTool_PrefersAnExplicitToolNameOverTheWorkflowName()
    {
        Tool tool = BuildTool(new WorkflowDefinition
        {
            DefinitionId = "49ba274a987b98d4",
            Name = "PR - Review task",
            CustomProperties = { ["mcp:name"] = "start_pr_review" }
        });

        Assert.Equal("start_pr_review", tool.Name);
    }

    [Fact]
    public void ToTool_KeepsTheWorkflowNameAsTheTitle()
    {
        Tool tool = BuildTool(new WorkflowDefinition { DefinitionId = "49ba274a987b98d4", Name = "PR - Review task" });

        Assert.Equal("PR - Review task", tool.Title);
    }

    [Fact]
    public void ToTool_AppendsTheInstructionsToTheDescription()
    {
        Tool tool = BuildTool(new WorkflowDefinition
        {
            DefinitionId = "abc123",
            Name = "PR - Review task",
            Description = "Create a review task for a new pull request.",
            CustomProperties = { ["mcp:instructions"] = "Use this for a pull request, not for a work item." }
        });

        Assert.Equal(
            "Create a review task for a new pull request.\n\nUse this for a pull request, not for a work item.",
            tool.Description);
    }

    [Fact]
    public void ToTool_FallsBackToTheWorkflowNameWhenTheDescriptionIsBlank()
    {
        // A blank description is what the designer leaves behind, and it reached the client as an empty string for as
        // long as the fallback only tested for null.
        Tool tool = BuildTool(new WorkflowDefinition { DefinitionId = "abc123", Name = "PR - Review task", Description = "   " });

        Assert.Equal("PR - Review task", tool.Description);
    }

    [Fact]
    public void ToTool_DescribesAnInputWithItsOwnInstruction()
    {
        // An input has no property bag of its own — ArgumentDefinition carries nothing but name, display name,
        // description, category and type — so the instruction is keyed by input name on the definition instead. It
        // wins over the description, which stays the text a person reads in Studio.
        Tool tool = BuildTool(new WorkflowDefinition
        {
            DefinitionId = "abc123",
            Name = "PR - Review task",
            Inputs = [new InputDefinition { Name = "PRId", Type = typeof(int), DisplayName = "Pullrequest Id", Description = "The number of the pull request." }],
            CustomProperties = { ["mcp:input:PRId"] = "The id of the pull request in Azure DevOps, not that of a work item." }
        });

        Assert.Equal(
            "The id of the pull request in Azure DevOps, not that of a work item.",
            InputDescription(tool, "PRId"));
    }

    [Fact]
    public void ToTool_MatchesAnInputInstructionRegardlessOfItsCasing()
    {
        Tool tool = BuildTool(new WorkflowDefinition
        {
            DefinitionId = "abc123",
            Name = "PR - Review task",
            Inputs = [new InputDefinition { Name = "PRId", Type = typeof(int), DisplayName = "Pullrequest Id" }],
            CustomProperties = { ["MCP:Input:prid"] = "The pull request id." }
        });

        Assert.Equal("The pull request id.", InputDescription(tool, "PRId"));
    }

    [Fact]
    public async Task ListToolsAsync_ExposesAWorkflowWhoseOptInKeyDiffersInCasing()
    {
        // The opt-in was first read with an exact key comparison while the instructions were not. One reader for all
        // four properties makes that consistent, and wider is the safe side here: an author who types "MCP:Enabled"
        // unmistakably means the workflow to be a tool.
        WorkflowToolCatalog catalog = BuildCatalog(new WorkflowDefinition
        {
            DefinitionId = "49ba274a987b98d4",
            Name = "PR - Review task",
            CustomProperties = { ["MCP:Enabled"] = true }
        });

        IReadOnlyList<Tool> tools = await catalog.ListToolsAsync(CancellationToken.None);

        Assert.Equal("pr-review-task", Assert.Single(tools).Name);
    }

    [Fact]
    public void ToTool_FallsBackToTheInputDescriptionWithoutAnInstruction()
    {
        Tool tool = BuildTool(new WorkflowDefinition
        {
            DefinitionId = "abc123",
            Name = "PR - Review task",
            Inputs = [new InputDefinition { Name = "PRId", Type = typeof(int), DisplayName = "Pullrequest Id", Description = "The number of the pull request." }]
        });

        Assert.Equal("The number of the pull request.", InputDescription(tool, "PRId"));
    }

    [Fact]
    public async Task ListToolsAsync_DisambiguatesWorkflowsThatShareAName()
    {
        // Two published workflows may carry the same name. Naming both after it would let a call land on whichever the
        // store happened to return first, so every participant in the clash keeps its id.
        WorkflowToolCatalog catalog = BuildCatalog(
            Exposed("aaa111", "PR - Review task"),
            Exposed("bbb222", "PR - Review task"),
            Exposed("ccc333", "List incidents"));

        IReadOnlyList<Tool> tools = await catalog.ListToolsAsync(CancellationToken.None);

        Assert.Equal(
            ["list-incidents", "pr-review-task-aaa111", "pr-review-task-bbb222"],
            tools.Select(tool => tool.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task FindExposedDefinitionAsync_ResolvesAToolByItsName()
    {
        WorkflowToolCatalog catalog = BuildCatalog(Exposed("49ba274a987b98d4", "PR - Review task"));

        WorkflowDefinition? definition = await catalog.FindExposedDefinitionAsync("pr-review-task", CancellationToken.None);

        Assert.Equal("49ba274a987b98d4", definition?.DefinitionId);
    }

    [Fact]
    public async Task FindExposedDefinitionAsync_ResolvesAToolByItsDisambiguatedName()
    {
        WorkflowToolCatalog catalog = BuildCatalog(
            Exposed("aaa111", "PR - Review task"),
            Exposed("bbb222", "PR - Review task"));

        WorkflowDefinition? definition = await catalog.FindExposedDefinitionAsync("pr-review-task-bbb222", CancellationToken.None);

        Assert.Equal("bbb222", definition?.DefinitionId);
    }

    [Fact]
    public async Task FindExposedDefinitionAsync_StillResolvesAToolByItsDefinitionId()
    {
        // A client that learned the hex name before this change keeps calling it, and unlike a name derived from an
        // editable field, an id never moves.
        WorkflowToolCatalog catalog = BuildCatalog(Exposed("49ba274a987b98d4", "PR - Review task"));

        WorkflowDefinition? definition = await catalog.FindExposedDefinitionAsync("49ba274a987b98d4", CancellationToken.None);

        Assert.Equal("49ba274a987b98d4", definition?.DefinitionId);
    }

    [Fact]
    public async Task FindExposedDefinitionAsync_DoesNotResolveAWorkflowThatIsNotExposed()
    {
        WorkflowToolCatalog catalog = BuildCatalog(new WorkflowDefinition { DefinitionId = "49ba274a987b98d4", Name = "PR - Review task" });

        Assert.Null(await catalog.FindExposedDefinitionAsync("pr-review-task", CancellationToken.None));
        Assert.Null(await catalog.FindExposedDefinitionAsync("49ba274a987b98d4", CancellationToken.None));
    }

    private static string? InputDescription(Tool tool, string inputName) =>
        tool.InputSchema.GetProperty("properties").GetProperty(inputName).GetProperty("description").GetString();

    private static WorkflowDefinition Exposed(string definitionId, string name) =>
        new()
        {
            DefinitionId = definitionId,
            Name = name,
            CustomProperties = { ["mcp:enabled"] = true }
        };

    private static WorkflowToolCatalog BuildCatalog(params WorkflowDefinition[] definitions)
    {
        IWorkflowDefinitionStore store = Substitute.For<IWorkflowDefinitionStore>();

        store.FindManyAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(definitions.AsEnumerable());

        store.FindAsync(Arg.Any<WorkflowDefinitionFilter>(), Arg.Any<CancellationToken>())
            .Returns(call =>
                definitions.FirstOrDefault(definition => definition.DefinitionId == call.Arg<WorkflowDefinitionFilter>().DefinitionId));

        return new WorkflowToolCatalog(store, Options.Create(new McpOptions()));
    }

    private static Tool BuildTool(WorkflowDefinition definition)
    {
        WorkflowToolCatalog catalog = new(Substitute.For<IWorkflowDefinitionStore>(), Options.Create(new McpOptions()));

        return catalog.ToTool(definition);
    }

    private static JsonElement BuildSchemaFor(Type inputType)
    {
        WorkflowToolCatalog catalog = new(Substitute.For<IWorkflowDefinitionStore>(), Options.Create(new McpOptions()));
        WorkflowDefinition definition = new()
        {
            DefinitionId = "TestWorkflow",
            Inputs = [new InputDefinition { Name = "Input", Type = inputType }]
        };

        Tool tool = catalog.ToTool(definition);

        return tool.InputSchema.GetProperty("properties").GetProperty("Input").Clone();
    }
}
