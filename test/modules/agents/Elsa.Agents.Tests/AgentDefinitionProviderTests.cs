using Elsa.Agents;
using Xunit;

namespace Elsa.Agents.Tests;

public class AgentDefinitionProviderTests
{
    [Fact]
    public void GetDefinitions_ReturnsRegisteredDefinitions()
    {
        // Arrange
        var definition1 = new TestAgentDefinition("Agent1");
        var definition2 = new TestAgentDefinition("Agent2");
        var definitions = new List<IAgentDefinition> { definition1, definition2 };
        var provider = new AgentDefinitionProvider(definitions);

        // Act
        var result = provider.GetDefinitions().ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(definition1, result);
        Assert.Contains(definition2, result);
    }

    [Fact]
    public void GetDefinitions_WithNoDefinitions_ReturnsEmpty()
    {
        // Arrange
        var provider = new AgentDefinitionProvider(Array.Empty<IAgentDefinition>());

        // Act
        var result = provider.GetDefinitions().ToList();

        // Assert
        Assert.Empty(result);
    }

    private class TestAgentDefinition : IAgentDefinition
    {
        public TestAgentDefinition(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public string Description => $"Test agent {Name}";

        public AgentConfig GetAgentConfig()
        {
            return new AgentConfig
            {
                Name = Name,
                Description = Description,
                Services = Array.Empty<string>(),
                FunctionName = "Test",
                PromptTemplate = "Test prompt",
                InputVariables = Array.Empty<InputVariableConfig>(),
                OutputVariable = new OutputVariableConfig()
            };
        }
    }
}
