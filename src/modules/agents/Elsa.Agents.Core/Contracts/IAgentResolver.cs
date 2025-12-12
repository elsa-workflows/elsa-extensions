namespace Elsa.Agents;

/// <summary>
/// Resolves IElsaAgent instances by name, regardless of whether they are
/// defined via Semantic Kernel configuration, code-first MAF agents, or
/// other provider-based sources.
/// </summary>
public interface IAgentResolver
{
    /// <summary>
    /// Resolve an agent by name. Throws if the agent cannot be found.
    /// </summary>
    Task<IElsaAgent> ResolveAsync(string name, CancellationToken cancellationToken = default);
}