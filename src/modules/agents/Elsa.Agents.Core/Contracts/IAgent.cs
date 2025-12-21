namespace Elsa.Agents;

/// <summary>
/// Minimal abstraction to represent a code-first agent that can be automatically discovered as an activity.
/// Implementing classes should define public async Task methods accepting <see cref="AgentExecutionContext"/> as a parameter.
/// Each such method will be automatically discovered and exposed as an activity.
/// </summary>
public interface IAgent;