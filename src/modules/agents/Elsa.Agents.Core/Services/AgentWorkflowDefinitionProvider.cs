namespace Elsa.Agents;

/// <summary>
/// Default implementation of <see cref="IAgentWorkflowDefinitionProvider"/> that collects all registered agent workflow definitions.
/// </summary>
public class AgentWorkflowDefinitionProvider : IAgentWorkflowDefinitionProvider
{
    private readonly IEnumerable<IAgentWorkflowDefinition> _definitions;

    public AgentWorkflowDefinitionProvider(IEnumerable<IAgentWorkflowDefinition> definitions)
    {
        _definitions = definitions;
    }

    /// <inheritdoc />
    public IEnumerable<IAgentWorkflowDefinition> GetDefinitions() => _definitions;
}
