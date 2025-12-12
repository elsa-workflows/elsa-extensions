using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0110

namespace Elsa.Agents;

/// <summary>
/// Factory for creating Agent Framework agents from Elsa agent configurations.
/// </summary>
public class AgentFrameworkFactory(
    IPluginDiscoverer pluginDiscoverer,
    IServiceDiscoverer serviceDiscoverer,
    ILoggerFactory loggerFactory,
    IServiceProvider serviceProvider,
    ILogger<AgentFrameworkFactory> logger)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

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
    /// Creates an IElsaAgent adapter for a given configuration, so callers can
    /// work against a unified abstraction regardless of the underlying implementation.
    /// </summary>
    public IElsaAgent CreateElsaAgent(KernelConfig kernelConfig, AgentConfig agentConfig)
    {
        var skAgent = CreateAgent(kernelConfig, agentConfig);
        return new SemanticKernelElsaAgent(skAgent);
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

        AddPlugins(builder, agentConfig);
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
    
    private void AddPlugins(IKernelBuilder builder, AgentConfig agent)
    {
        var plugins = pluginDiscoverer.GetPluginDescriptors().ToDictionary(x => x.Name);
        foreach (var pluginName in agent.Plugins)
        {
            if (!plugins.TryGetValue(pluginName, out var pluginDescriptor))
            {
                logger.LogWarning($"Plugin {pluginName} not found");
                continue;
            }

            var pluginType = pluginDescriptor.PluginType;
            var pluginInstance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, pluginType);
            builder.Plugins.AddFromObject(pluginInstance, pluginName);
        }
    }
}
