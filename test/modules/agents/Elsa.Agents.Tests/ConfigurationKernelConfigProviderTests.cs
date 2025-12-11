using Elsa.Agents;
using Microsoft.Extensions.Options;
using Xunit;

namespace Elsa.Agents.Tests;

public class ConfigurationKernelConfigProviderTests
{
    [Fact]
    public async Task GetKernelConfigAsync_MergesConfigurationAndCodeFirstAgents()
    {
        // Arrange
        var options = Options.Create(new AgentsOptions
        {
            Agents = new List<AgentConfig>
            {
                new() { Name = "ConfigAgent", Description = "From config" }
            }
        });

        var codeFirstAgent = new TestAgent("CodeFirstAgent");
        var agentProvider = new AgentDefinitionProvider(new[] { codeFirstAgent });
        var workflowProvider = new AgentWorkflowDefinitionProvider(Array.Empty<IAgentWorkflowDefinition>());
        
        var provider = new ConfigurationKernelConfigProvider(options, agentProvider, workflowProvider);

        // Act
        var config = await provider.GetKernelConfigAsync();

        // Assert
        Assert.Equal(2, config.Agents.Count);
        Assert.True(config.Agents.ContainsKey("ConfigAgent"));
        Assert.True(config.Agents.ContainsKey("CodeFirstAgent"));
    }

    [Fact]
    public async Task GetKernelConfigAsync_IncludesAgentWorkflows()
    {
        // Arrange
        var options = Options.Create(new AgentsOptions());
        var agentProvider = new AgentDefinitionProvider(Array.Empty<IAgentDefinition>());
        
        var workflow = new TestWorkflow("TestWorkflow");
        var workflowProvider = new AgentWorkflowDefinitionProvider(new[] { workflow });
        
        var provider = new ConfigurationKernelConfigProvider(options, agentProvider, workflowProvider);

        // Act
        var config = await provider.GetKernelConfigAsync();

        // Assert
        Assert.Single(config.AgentWorkflows);
        Assert.True(config.AgentWorkflows.ContainsKey("TestWorkflow"));
    }

    private class TestAgent : IAgentDefinition
    {
        public TestAgent(string name) => Name = name;
        public string Name { get; }
        public string Description => $"Test {Name}";
        public AgentConfig GetAgentConfig() => new()
        {
            Name = Name,
            Description = Description,
            Services = Array.Empty<string>(),
            FunctionName = "Test",
            PromptTemplate = "Test",
            InputVariables = Array.Empty<InputVariableConfig>(),
            OutputVariable = new OutputVariableConfig()
        };
    }

    private class TestWorkflow : IAgentWorkflowDefinition
    {
        public TestWorkflow(string name) => Name = name;
        public string Name { get; }
        public string Description => $"Test {Name}";
        public AgentWorkflowConfig GetWorkflowConfig() => new()
        {
            Name = Name,
            Description = Description,
            WorkflowType = AgentWorkflowType.Sequential,
            Agents = Array.Empty<string>(),
            Services = Array.Empty<string>(),
            InputVariables = Array.Empty<InputVariableConfig>(),
            OutputVariable = new OutputVariableConfig()
        };
    }
}
