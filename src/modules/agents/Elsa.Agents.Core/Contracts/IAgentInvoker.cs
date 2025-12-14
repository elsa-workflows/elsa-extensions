namespace Elsa.Agents;

/// <summary>
/// Invokes an agent using the Microsoft Agent Framework.
/// </summary>
public interface IAgentInvoker
{
    /// <summary>
    /// Invokes an agent using the Microsoft Agent Framework.
    /// </summary>
    Task<InvokeAgentResult> InvokeAgentAsync(string agentName, IDictionary<string, object?> input, CancellationToken cancellationToken = default);
}
