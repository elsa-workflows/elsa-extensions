namespace Elsa.Agents;

public static class CodeFirstAgentResolverExtensions
{
    public static async Task<TAgent> ResolveAsync<TAgent>(this IAgentResolver resolver, CancellationToken cancellationToken = default) where TAgent : IAgent
    {
        var agentName = typeof(TAgent).Name;
        return (TAgent)await resolver.ResolveAsync(agentName, cancellationToken);
    }
}