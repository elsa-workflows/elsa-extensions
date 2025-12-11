namespace Elsa.Agents;

/// <summary>
/// Default implementation of <see cref="IAgentDefinitionProvider"/> that collects all registered agent definitions.
/// </summary>
public class AgentDefinitionProvider : IAgentDefinitionProvider
{
    private readonly IEnumerable<IAgentDefinition> _definitions;

    public AgentDefinitionProvider(IEnumerable<IAgentDefinition> definitions)
    {
        _definitions = definitions;
    }

    /// <inheritdoc />
    public IEnumerable<IAgentDefinition> GetDefinitions() => _definitions;
}
