using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Elsa.Agents;

/// <summary>
/// Provides kernel configuration by merging configuration-based agents with code-first definitions.
/// </summary>
[UsedImplicitly]
public class ConfigurationKernelConfigProvider(
    IOptions<AgentsOptions> options,
    IAgentDefinitionProvider agentDefinitionProvider,
    IAgentWorkflowDefinitionProvider workflowDefinitionProvider) : IKernelConfigProvider
{
    public Task<KernelConfig> GetKernelConfigAsync(CancellationToken cancellationToken = default)
    {
        var kernelConfig = new KernelConfig();
        
        // Add configuration-based items
        foreach (var apiKey in options.Value.ApiKeys) 
            kernelConfig.ApiKeys[apiKey.Name] = apiKey;
        foreach (var service in options.Value.Services) 
            kernelConfig.Services[service.Name] = service;
        foreach (var agent in options.Value.Agents) 
            kernelConfig.Agents[agent.Name] = agent;
        
        // Add code-first agents
        foreach (var agentDef in agentDefinitionProvider.GetDefinitions())
        {
            var agentConfig = agentDef.GetAgentConfig();
            kernelConfig.Agents[agentDef.Name] = agentConfig;
        }
        
        // Add code-first agent workflows
        foreach (var workflowDef in workflowDefinitionProvider.GetDefinitions())
        {
            var workflowConfig = workflowDef.GetWorkflowConfig();
            kernelConfig.AgentWorkflows[workflowDef.Name] = workflowConfig;
        }
        
        return Task.FromResult(kernelConfig);
    }
}