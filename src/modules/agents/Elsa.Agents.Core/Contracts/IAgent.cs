using Microsoft.Agents.AI;

namespace Elsa.Agents;

/// <summary>
/// Minimal abstraction to represent a code-first agent that can be automatically discovered as an activity.
/// </summary>
public interface IAgent
{
    /// <summary>
    /// Executes the agent with the given context and returns the primary text result.
    /// </summary>
    Task<AgentRunResponse> RunAsync(AgentExecutionContext context);
}