using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0110

namespace Elsa.Agents;

/// <summary>
/// Factory for creating Agent Framework agents from Elsa agent configurations.
/// </summary>
public class AgentFactory(
    ISkillDiscoverer skillDiscoverer,
    IServiceDiscoverer serviceDiscoverer,
    IServiceProvider serviceProvider,
    ILogger<AgentFactory> logger)
{
    /// <summary>
    /// Creates a ChatCompletionAgent from an Elsa agent configuration.
    /// </summary>
    public ChatCompletionAgent CreateAgent(KernelConfig kernelConfig, AgentConfig agentConfig)
    {
        var kernel = CreateKernel(kernelConfig, agentConfig);
        
        return new()
        {
            Name = agentConfig.Name,
            Description = agentConfig.Description,
            Instructions = agentConfig.PromptTemplate,
            Kernel = kernel
        };
    }

    /// <summary>
    /// Creates a Kernel configured for the specified agent.
    /// </summary>
    private Kernel CreateKernel(KernelConfig kernelConfig, AgentConfig agentConfig)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
        builder.Services.AddSingleton(agentConfig);

        ApplyAgentConfig(builder, kernelConfig, agentConfig);

        return builder.Build();
    }

    private void ApplyAgentConfig(IKernelBuilder builder, KernelConfig kernelConfig, AgentConfig agentConfig)
    {
        var services = serviceDiscoverer.Discover().ToDictionary(x => x.Name);
        
        foreach (string serviceName in agentConfig.Services)
        {
            if (!kernelConfig.Services.TryGetValue(serviceName, out var serviceConfig))
            {
                logger.LogWarning($"Service {serviceName} not found");
                continue;
            }

            AddService(builder, kernelConfig, serviceConfig, services);
        }

        AddSkills(builder, agentConfig);
    }

    private void AddService(IKernelBuilder builder, KernelConfig kernelConfig, ServiceConfig serviceConfig, Dictionary<string, IAgentServiceProvider> services)
    {
        if (!services.TryGetValue(serviceConfig.Type, out var serviceProvider))
        {
            logger.LogWarning($"Service provider {serviceConfig.Type} not found");
            return;
        }
        
        var context = new KernelBuilderContext(builder, kernelConfig, serviceConfig);
        serviceProvider.ConfigureKernel(context);
    }
    
    private void AddSkills(IKernelBuilder builder, AgentConfig agent)
    {
        var skills = skillDiscoverer.DiscoverSkills().ToDictionary(x => x.Name);
        foreach (var skillName in agent.Skills)
        {
            if (!skills.TryGetValue(skillName, out var skillDescriptor))
            {
                logger.LogWarning($"Skill {skillName} not found");
                continue;
            }

            var clrType = skillDescriptor.ClrType;
            var skillInstance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, clrType);
            builder.Plugins.AddFromObject(skillInstance, skillName);
        }
    }
}
