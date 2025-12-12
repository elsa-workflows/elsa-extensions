namespace Elsa.Agents;

/// <summary>
/// Contract for pluggable agent providers that can contribute agents to
/// the resolver. Each provider decides which names it supports.
/// </summary>
public interface IAgentProvider
{
    /// <summary>
    /// Returns true if this provider can supply an agent for the specified name.
    /// </summary>
    Task<bool> CanProvideAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an IElsaAgent for the specified name. Only called if
    /// <see cref="CanProvideAsync"/> returned true.
    /// </summary>
    Task<IElsaAgent> CreateAsync(string name, CancellationToken cancellationToken = default);
}