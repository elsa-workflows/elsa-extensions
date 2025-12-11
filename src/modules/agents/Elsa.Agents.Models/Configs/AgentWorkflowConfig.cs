namespace Elsa.Agents;

/// <summary>
/// Configuration for a multi-agent workflow (team/sequence/graph).
/// </summary>
public class AgentWorkflowConfig
{
    /// <summary>
    /// The name of the agent workflow.
    /// </summary>
    public string Name { get; set; } = "";
    
    /// <summary>
    /// The description of the agent workflow.
    /// </summary>
    public string Description { get; set; } = "";
    
    /// <summary>
    /// The type of workflow orchestration (Sequential, Parallel, Graph).
    /// </summary>
    public AgentWorkflowType WorkflowType { get; set; } = AgentWorkflowType.Sequential;
    
    /// <summary>
    /// The agents participating in this workflow.
    /// </summary>
    public ICollection<string> Agents { get; set; } = [];
    
    /// <summary>
    /// Services required by the workflow.
    /// </summary>
    public ICollection<string> Services { get; set; } = [];
    
    /// <summary>
    /// Input variables for the workflow.
    /// </summary>
    public ICollection<InputVariableConfig> InputVariables { get; set; } = [];
    
    /// <summary>
    /// Output variable for the workflow.
    /// </summary>
    public OutputVariableConfig OutputVariable { get; set; } = new();
    
    /// <summary>
    /// Execution settings for the workflow.
    /// </summary>
    public ExecutionSettingsConfig ExecutionSettings { get; set; } = new();
    
    /// <summary>
    /// The termination strategy for the workflow (e.g., after N messages, on specific condition).
    /// </summary>
    public TerminationConfig Termination { get; set; } = new();
    
    /// <summary>
    /// Optional selection strategy configuration for determining which agent acts next.
    /// </summary>
    public SelectionStrategyConfig? SelectionStrategy { get; set; }
}

/// <summary>
/// Types of agent workflow orchestration.
/// </summary>
public enum AgentWorkflowType
{
    /// <summary>
    /// Agents execute sequentially in order.
    /// </summary>
    Sequential,
    
    /// <summary>
    /// Custom graph-based orchestration where agent selection is determined by a strategy.
    /// </summary>
    Graph
}
