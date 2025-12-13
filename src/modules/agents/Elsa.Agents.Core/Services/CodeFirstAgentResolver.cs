using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Agents;

public class CodeFirstAgentResolver(IServiceProvider serviceProvider, IOptions<CodeFirstAgentOptions> options) : ICodeFirstAgentResolver
{
    public Task<IElsaAgent> ResolveAsync(string agentName, CancellationToken cancellationToken = default)
    {
        var agentType = options.Value.CodeFirstAgents[agentName] ?? throw new InvalidOperationException($"No agent with name '{agentName}' was found.");
        var agent = (IElsaAgent)ActivatorUtilities.CreateInstance(serviceProvider, agentType)!;
        return Task.FromResult(agent);
    }
}