namespace Elsa.Agents;

/// <summary>
/// Provides access to registered agent definitions.
/// </summary>
public interface IAgentDefinitionProvider
{
    /// <summary>
    /// Gets all registered agent definitions.
    /// </summary>
    IEnumerable<IAgentDefinition> GetDefinitions();
}
