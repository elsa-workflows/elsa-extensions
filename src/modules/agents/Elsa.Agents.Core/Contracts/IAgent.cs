using Microsoft.Agents.AI;

namespace Elsa.Agents;

/// <summary>
/// Minimal abstraction over an executable agent so activities and endpoints
/// do not need to know whether the underlying implementation is SK-based,
/// ChatClientAgent-based, or something else.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Executes the agent with the given context and returns the primary text result.
    /// </summary>
    Task<AgentRunResponse> RunAsync(AgentExecutionContext context);
}