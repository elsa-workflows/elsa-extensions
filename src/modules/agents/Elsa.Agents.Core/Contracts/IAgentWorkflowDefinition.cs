namespace Elsa.Agents;

/// <summary>
/// Represents a code-first multi-agent workflow (agent team/sequence/graph) that can be registered programmatically.
/// </summary>
public interface IAgentWorkflowDefinition
{
    /// <summary>
    /// Gets the unique name of the agent workflow.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets the description of the agent workflow.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Gets the agent workflow configuration.
    /// </summary>
    AgentWorkflowConfig GetWorkflowConfig();
}
