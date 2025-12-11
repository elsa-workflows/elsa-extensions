namespace Elsa.Agents;

/// <summary>
/// Provides access to registered agent workflow definitions.
/// </summary>
public interface IAgentWorkflowDefinitionProvider
{
    /// <summary>
    /// Gets all registered agent workflow definitions.
    /// </summary>
    IEnumerable<IAgentWorkflowDefinition> GetDefinitions();
}
