using Elsa.Mcp.Abstractions;

namespace Elsa.Mcp.Abstractions.UnitTests;

public class McpToolNameTests
{
    // A tool name is derived from the workflow name rather than from the definition id. A workflow drawn in Studio
    // is named after a generated hex string, so a client picking a tool by id picks blind: a request to review a
    // pull request would start the work item assistant instead, with no error to show for it.
    [Theory]
    [InlineData("PR - Review task", "pr-review-task")]
    [InlineData("Workitem Assistant Starter", "workitem-assistant-starter")]
    [InlineData("Add workitem comment", "add-workitem-comment")]
    [InlineData("Start agent on work item", "start-agent-on-work-item")]
    [InlineData("List running versions", "list-running-versions")]
    public void FromWorkflowName_DerivesTheToolNameFromTheWorkflowName(string workflowName, string expected)
    {
        Assert.Equal(expected, McpToolName.FromWorkflowName(workflowName));
    }

    [Fact]
    public void FromWorkflowName_FoldsAccentsRatherThanDroppingThem()
    {
        // Without folding, an accented vowel is dropped instead, and the tool ends up named after the wrong word:
        // the Dutch word in the name below would collapse to "n".
        Assert.Equal("zorg-welzijn-een-v2", McpToolName.FromWorkflowName("Zorg & Welzijn: één (v2)"));
    }

    [Fact]
    public void FromWorkflowName_CollapsesRunsOfSeparatorsIntoOne()
    {
        Assert.Equal("a-b", McpToolName.FromWorkflowName("  a  ---  b  "));
    }

    [Fact]
    public void FromWorkflowName_GivesNoNameToOneThatReducesToNothing()
    {
        Assert.Null(McpToolName.FromWorkflowName("→ ✓"));
        Assert.Null(McpToolName.FromWorkflowName("   "));
        Assert.Null(McpToolName.FromWorkflowName(null));
    }

    [Fact]
    public void FromWorkflowName_KeepsTheNameWithinTheClientLimit()
    {
        string name = McpToolName.FromWorkflowName(new string('a', 40) + " " + new string('b', 40))!;

        Assert.Equal(64, name.Length);
        Assert.DoesNotContain("--", name);
        Assert.False(name.EndsWith('-'));
    }

    [Theory]
    [InlineData("start_pr_review", true)]
    [InlineData("pr-review-task", true)]
    [InlineData("PRReview2", true)]
    [InlineData("pr review", false)]
    [InlineData("mcp:naam", false)]
    [InlineData("", false)]
    public void IsAcceptable_AcceptsOnlyWhatAClientWillCall(string name, bool expected)
    {
        Assert.Equal(expected, McpToolName.IsAcceptable(name));
    }

    [Fact]
    public void IsAcceptable_RejectsANameOverTheLimit()
    {
        Assert.True(McpToolName.IsAcceptable(new string('a', 64)));
        Assert.False(McpToolName.IsAcceptable(new string('a', 65)));
    }

    [Fact]
    public void Disambiguate_AppendsTheDefinitionId()
    {
        Assert.Equal("pr-review-task-aaa111", McpToolName.Disambiguate("pr-review-task", "aaa111"));
    }

    [Fact]
    public void Disambiguate_TrimsTheNameRatherThanTheId()
    {
        // The id is what makes the result unique, so that is the half that must not be truncated.
        string definitionId = new string('9', 20);

        string name = McpToolName.Disambiguate(new string('a', 60), definitionId);

        Assert.Equal(64, name.Length);
        Assert.EndsWith(definitionId, name);
        Assert.DoesNotContain("--", name);
    }

    [Fact]
    public void Disambiguate_FallsBackToTheIdWhenThereIsNoRoomForAName()
    {
        string definitionId = new string('9', 64);

        Assert.Equal(definitionId, McpToolName.Disambiguate("pr-review-task", definitionId));
    }
}
