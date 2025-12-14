namespace Elsa.Agents;

public class AgentsOptions
{
    public ICollection<AgentConfig> Agents { get; set; } = new List<AgentConfig>();
    public ICollection<ServiceDescriptor> ServiceDescriptors { get; set; } = new List<ServiceDescriptor>();
    
    /// <summary>
    /// Map from agent key to the implementing type. Keys are case-insensitive.
    /// </summary>
    public IDictionary<string, Type> AgentTypes { get; } = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers a code-first agent type. If no key is provided, the type name
    /// is used as the key.
    /// </summary>
    public AgentsOptions AddAgentType<TAgent>(string? key = null) where TAgent : class, IAgent
    {
        key ??= typeof(TAgent).Name;
        AgentTypes[key] = typeof(TAgent);
        return this;
    }
}