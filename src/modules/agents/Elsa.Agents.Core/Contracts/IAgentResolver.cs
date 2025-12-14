namespace Elsa.Agents;

/// <summary>
/// Defines the contract for resolving agents by their names.
/// </summary>
public interface IAgentResolver
{
    /// <summary>
    /// Resolves an agent instance by its name asynchronously.
    /// </summary>
    /// <param name="agentName">The name of the agent to resolve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The resolved agent instance.</returns>
    Task<IAgent> ResolveAsync(string agentName, CancellationToken cancellationToken = default);
}