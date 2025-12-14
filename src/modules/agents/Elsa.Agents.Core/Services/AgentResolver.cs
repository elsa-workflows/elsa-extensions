using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Agents;

public class AgentResolver(IServiceProvider serviceProvider, IOptions<AgentOptions> options) : IAgentResolver
{
    public Task<IAgent> ResolveAsync(string agentName, CancellationToken cancellationToken = default)
    {
        var agentType = options.Value.AgentTypes[agentName] ?? throw new InvalidOperationException($"No agent with name '{agentName}' was found.");
        var agent = (IAgent)ActivatorUtilities.CreateInstance(serviceProvider, agentType)!;
        return Task.FromResult(agent);
    }
}