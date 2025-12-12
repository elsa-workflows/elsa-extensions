using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Agents;

/// <summary>
/// Agent provider that exposes code-first agents registered via AgentOptions.
/// Types must be registered in DI and implement IElsaAgent.
/// </summary>
public class CodeFirstAgentProvider(IOptions<CodeFirstAgentOptions> options, IServiceProvider serviceProvider) : IAgentProvider
{
    public Task<bool> CanProvideAsync(string name, CancellationToken cancellationToken = default)
    {
        var agents = options.Value.CodeFirstAgents;
        var canProvide = agents.ContainsKey(name);
        return Task.FromResult(canProvide);
    }

    public Task<IElsaAgent> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var agents = options.Value.CodeFirstAgents;

        if (!agents.TryGetValue(name, out var agentType))
            throw new InvalidOperationException($"No code-first agent registered for key '{name}'.");

        var instance = serviceProvider.GetRequiredService(agentType);

        if (instance is not IElsaAgent elsaAgent)
            throw new InvalidOperationException(
                $"Type '{agentType.FullName}' registered for key '{name}' does not implement IElsaAgent.");

        return Task.FromResult(elsaAgent);
    }
}

