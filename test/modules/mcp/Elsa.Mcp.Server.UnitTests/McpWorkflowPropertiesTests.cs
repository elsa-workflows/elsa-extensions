using Elsa.Mcp.Server.Models;

namespace Elsa.Mcp.Server.UnitTests;

public class McpWorkflowPropertiesTests
{
    [Theory]
    [InlineData(McpWorkflowProperties.AuthorizationToken)]
    [InlineData(McpWorkflowProperties.UserName)]
    [InlineData(McpWorkflowProperties.AgentDepth)]
    public void TreatsEveryCallerContextKeyAsUnwritableByWorkflowInput(string name)
    {
        // WorkflowToolInvoker skips these when mirroring input onto the instance properties, so a tool call cannot
        // pass itself off as another caller. AgentDepth belongs here for the same reason the other two do: a call
        // that could declare itself to be at depth zero would restart the recursion budget on every hop, which is
        // exactly what the counter exists to prevent.
        Assert.True(McpWorkflowProperties.IsCallerContextProperty(name));
    }

    [Theory]
    [InlineData("agentdepth")]
    [InlineData("AGENTDEPTH")]
    public void MatchesACallerContextKeyWhateverItsCasing(string name)
    {
        // Instance properties are compared case-insensitively elsewhere, so a case-sensitive check here would leave a
        // way round the guard.
        Assert.True(McpWorkflowProperties.IsCallerContextProperty(name));
    }

    [Theory]
    [InlineData("environment")]
    [InlineData("count")]
    [InlineData("AgentDepthLimit")]
    public void LeavesOrdinaryWorkflowInputAlone(string name)
    {
        Assert.False(McpWorkflowProperties.IsCallerContextProperty(name));
    }

    [Fact]
    public void NamesTheHeaderTheAgentPackageSends()
    {
        // Elsa.Ai.Agent deliberately does not reference this package, so it repeats this literal in
        // AgentWorkflowProperties. Asserting it on both sides turns a rename into a test failure rather than into a
        // depth counter that silently always reads zero.
        Assert.Equal("X-Elsa-Agent-Depth", McpWorkflowProperties.AgentDepthHeader);
        Assert.Equal("AgentDepth", McpWorkflowProperties.AgentDepth);
    }
}
