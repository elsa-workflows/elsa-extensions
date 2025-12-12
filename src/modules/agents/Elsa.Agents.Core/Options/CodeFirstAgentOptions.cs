namespace Elsa.Agents;

/// <summary>
/// Options used to configure code-first agents. Developers can register
/// agent types here and have them exposed via IAgentProvider/IAgentResolver.
/// </summary>
public class CodeFirstAgentOptions
{
    /// <summary>
    /// Map from agent key to the implementing type. Keys are case-insensitive.
    /// </summary>
    public IDictionary<string, Type> CodeFirstAgents { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a code-first agent type. If no key is provided, the type name
    /// is used as the key.
    /// </summary>
    public CodeFirstAgentOptions AddAgent<TAgent>(string? key = null) where TAgent : class, IElsaAgent
    {
        key ??= typeof(TAgent).Name;
        CodeFirstAgents[key] = typeof(TAgent);
        return this;
    }
}