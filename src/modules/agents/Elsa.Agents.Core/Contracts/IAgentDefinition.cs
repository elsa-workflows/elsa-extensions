namespace Elsa.Agents;

/// <summary>
/// Represents a code-first agent definition that can be registered programmatically.
/// </summary>
public interface IAgentDefinition
{
    /// <summary>
    /// Gets the unique name of the agent.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the description of the agent.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Gets the agent configuration.
    /// </summary>
    AgentConfig GetAgentConfig();
}
