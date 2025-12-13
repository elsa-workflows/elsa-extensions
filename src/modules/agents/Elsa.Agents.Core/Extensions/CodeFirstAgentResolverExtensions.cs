namespace Elsa.Agents;

public static class CodeFirstAgentResolverExtensions
{
    public static async Task<TAgent> ResolveAsync<TAgent>(this ICodeFirstAgentResolver resolver, CancellationToken cancellationToken = default) where TAgent : IElsaAgent
    {
        var agentName = typeof(TAgent).Name;
        return (TAgent)await resolver.ResolveAsync(agentName, cancellationToken);
    }
}