using Elsa.Agents;
using Xunit;

namespace Elsa.Agents.Tests;

public class AgentWorkflowDefinitionProviderTests
{
    [Fact]
    public void GetDefinitions_ReturnsRegisteredWorkflows()
    {
        // Arrange
        var workflow1 = new TestWorkflowDefinition("Workflow1");
        var workflow2 = new TestWorkflowDefinition("Workflow2");
        var definitions = new List<IAgentWorkflowDefinition> { workflow1, workflow2 };
        var provider = new AgentWorkflowDefinitionProvider(definitions);

        // Act
        var result = provider.GetDefinitions().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(workflow1, result);
        Assert.Contains(workflow2, result);
    }

    [Fact]
    public void GetDefinitions_WithNoWorkflows_ReturnsEmpty()
    {
        // Arrange
        var provider = new AgentWorkflowDefinitionProvider(Array.Empty<IAgentWorkflowDefinition>());

        // Act
        var result = provider.GetDefinitions().ToList();

        // Assert
        Assert.Empty(result);
    }

    private class TestWorkflowDefinition(string name) : IAgentWorkflowDefinition
    {
        public string Name { get; } = name;
        public string Description => $"Test workflow {Name}";

        public AgentWorkflowConfig GetWorkflowConfig()
        {
            return new()
            {
                Name = Name,
                Description = Description,
                WorkflowType = AgentWorkflowType.Sequential,
                Agents = Array.Empty<string>(),
                Services = Array.Empty<string>(),
                InputVariables = Array.Empty<InputVariableConfig>(),
                OutputVariable = new()
            };
        }
    }
}
