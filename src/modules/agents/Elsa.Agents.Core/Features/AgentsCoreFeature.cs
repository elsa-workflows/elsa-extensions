using Elsa.Agents.Skills;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents.Features;

/// <summary>
/// A feature that installs API endpoints to interact with skilled agents.
/// </summary>
[UsedImplicitly]
public class AgentsCoreFeature(IModule module) : FeatureBase(module)
{
    private Func<IServiceProvider, IKernelConfigProvider> _kernelConfigProviderFactory = sp => sp.GetRequiredService<ConfigurationKernelConfigProvider>();
    
    public AgentsCoreFeature UseKernelConfigProvider(Func<IServiceProvider, IKernelConfigProvider> factory)
    {
        _kernelConfigProviderFactory = factory;
        return this;
    }
    
    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddOptions<AgentOptions>();

        Services
            .AddScoped<IAgentInvoker, AgentInvoker>()
            .AddScoped<IAgentFactory, AgentFactory>()
            .AddScoped<ISkillDiscoverer, SkillDiscoverer>()
            .AddScoped(_kernelConfigProviderFactory)
            .AddScoped<ConfigurationKernelConfigProvider>()
            .AddScoped<IAgentResolver, AgentResolver>()
            .AddSkillsProvider<ImageGeneratorSkillsProvider>()
            .AddSkillsProvider<DocumentQuerySkillsProvider>()
            ;
    }
}