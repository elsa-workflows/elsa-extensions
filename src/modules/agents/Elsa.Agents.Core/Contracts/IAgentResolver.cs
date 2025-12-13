namespace Elsa.Agents;

public interface IAgentResolver
{
    Task<IAgent> ResolveAsync(string agentName, CancellationToken cancellationToken = default);
}