namespace Elsa.Agents;

/// <summary>
/// Configuration for agent workflow termination conditions.
/// </summary>
public class TerminationConfig
{
    /// <summary>
    /// The type of termination strategy.
    /// </summary>
    public TerminationType Type { get; set; } = TerminationType.MaxMessages;
    
    /// <summary>
    /// Maximum number of messages/turns before termination (when Type is MaxMessages).
    /// </summary>
    public int MaxMessages { get; set; } = 10;
    
    /// <summary>
    /// Keyword or pattern that triggers termination (when Type is Keyword).
    /// </summary>
    public string? TerminationKeyword { get; set; }
    
    /// <summary>
    /// Name of the agent that can trigger termination (when Type is AgentDecision).
    /// </summary>
    public string? TerminationAgentName { get; set; }
}

/// <summary>
/// Types of termination strategies for agent workflows.
/// </summary>
public enum TerminationType
{
    /// <summary>
    /// Terminate after a maximum number of messages/turns.
    /// </summary>
    MaxMessages,
    
    /// <summary>
    /// Terminate when a specific keyword or pattern is detected.
    /// </summary>
    Keyword,
    
    /// <summary>
    /// Allow a specific agent to decide when to terminate.
    /// </summary>
    AgentDecision
}
