using Elsa.Agents.Plugins;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Agents.Features;

/// <summary>
/// A feature that installs API endpoints to interact with skilled agents.
/// </summary>
[UsedImplicitly]
public class AgentsFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IKernelConfigProvider> _kernelConfigProviderFactory = sp => sp.GetRequiredService<ConfigurationKernelConfigProvider>();
    
    public AgentsFeature UseKernelConfigProvider(Func<IServiceProvider, IKernelConfigProvider> factory)
    {
        _kernelConfigProviderFactory = factory;
        return this;
    }
    
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddOptions<ConfiguredAgentOptions>();
        Services.AddOptions<CodeFirstAgentOptions>();

        Services
            .AddScoped<AgentInvoker>()
            .AddScoped<AgentFrameworkFactory>()
            .AddScoped<IPluginDiscoverer, PluginDiscoverer>()
            .AddScoped<IServiceDiscoverer, ServiceDiscoverer>()
            .AddScoped(_kernelConfigProviderFactory)
            .AddScoped<ConfigurationKernelConfigProvider>()
            .AddScoped<ICodeFirstAgentResolver, CodeFirstAgentResolver>()
            .AddPluginProvider<ImageGeneratorPluginProvider>()
            .AddPluginProvider<DocumentQueryPluginProvider>()
            .AddAgentServiceProvider<OpenAIChatCompletionProvider>()
            .AddAgentServiceProvider<OpenAITextToImageProvider>()
            .AddAgentServiceProvider<OpenAIEmbeddingGenerator>()
            ;
    }
}