using Elsa.Agents.Activities.Features;
using Elsa.Agents.Features;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Agents;

[DependsOn(typeof(AgentsCoreFeature))]
[DependsOn(typeof(AgentActivitiesFeature))]
public class AgentsFeature(IModule module) : FeatureBase(module)
{
    public AgentsFeature AddAgent<TAgent>(string? key = null) where TAgent : class, IAgent
    {
        Module.Services.Configure<AgentOptions>(options => options.AddAgentType<TAgent>(key));
        return this;
    }

    public AgentsFeature AddServiceDescriptor(ServiceDescriptor descriptor)
    {
        Module.Services.Configure<AgentOptions>(options => options.ServiceDescriptors.Add(descriptor));
        return this;
    }
}