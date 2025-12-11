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
public class AgentFrameworkFactory
{
    private readonly IPluginDiscoverer _pluginDiscoverer;
    private readonly IServiceDiscoverer _serviceDiscoverer;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentFrameworkFactory> _logger;

    public AgentFrameworkFactory(
        IPluginDiscoverer pluginDiscoverer,
        IServiceDiscoverer serviceDiscoverer,
        ILoggerFactory loggerFactory,
        IServiceProvider serviceProvider,
        ILogger<AgentFrameworkFactory> logger)
    {
        _pluginDiscoverer = pluginDiscoverer;
        _serviceDiscoverer = serviceDiscoverer;
        _loggerFactory = loggerFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Creates a ChatCompletionAgent from an Elsa agent configuration.
    /// </summary>
    public ChatCompletionAgent CreateAgent(KernelConfig kernelConfig, AgentConfig agentConfig)
    {
        var kernel = CreateKernel(kernelConfig, agentConfig);
        
        return new ChatCompletionAgent
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
        var services = _serviceDiscoverer.Discover().ToDictionary(x => x.Name);
        
        foreach (string serviceName in agentConfig.Services)
        {
            if (!kernelConfig.Services.TryGetValue(serviceName, out var serviceConfig))
            {
                _logger.LogWarning($"Service {serviceName} not found");
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
            _logger.LogWarning($"Service provider {serviceConfig.Type} not found");
            return;
        }
        
        var context = new KernelBuilderContext(builder, kernelConfig, serviceConfig);
        serviceProvider.ConfigureKernel(context);
    }
    
    private void AddPlugins(IKernelBuilder builder, AgentConfig agent)
    {
        var plugins = _pluginDiscoverer.GetPluginDescriptors().ToDictionary(x => x.Name);
        foreach (var pluginName in agent.Plugins)
        {
            if (!plugins.TryGetValue(pluginName, out var pluginDescriptor))
            {
                _logger.LogWarning($"Plugin {pluginName} not found");
                continue;
            }

            var pluginType = pluginDescriptor.PluginType;
            var pluginInstance = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, pluginType);
            builder.Plugins.AddFromObject(pluginInstance, pluginName);
        }
    }
}
