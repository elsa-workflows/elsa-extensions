namespace Elsa.Agents;

/// <summary>
/// Configuration for agent selection strategy in multi-agent workflows.
/// </summary>
public class SelectionStrategyConfig
{
    /// <summary>
    /// The type of selection strategy.
    /// </summary>
    public SelectionStrategyType Type { get; set; } = SelectionStrategyType.Sequential;
    
    /// <summary>
    /// Custom selection prompt (when Type is LLMBased).
    /// </summary>
    public string? SelectionPrompt { get; set; }
    
    /// <summary>
    /// Agent responsible for making selection decisions (when Type is AgentBased).
    /// </summary>
    public string? SelectorAgentName { get; set; }
}

/// <summary>
/// Types of agent selection strategies.
/// </summary>
public enum SelectionStrategyType
{
    /// <summary>
    /// Agents are selected in sequential order.
    /// </summary>
    Sequential,
    
    /// <summary>
    /// Round-robin selection among agents.
    /// </summary>
    RoundRobin,
    
    /// <summary>
    /// Use an LLM to decide which agent should act next.
    /// </summary>
    LLMBased,
    
    /// <summary>
    /// Use a dedicated agent to make selection decisions.
    /// </summary>
    AgentBased
}
