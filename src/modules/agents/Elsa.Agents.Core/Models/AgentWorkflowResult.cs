using Microsoft.SemanticKernel.ChatCompletion;

namespace Elsa.Agents;

/// <summary>
/// Result of executing an agent workflow.
/// </summary>
public class AgentWorkflowResult(AgentWorkflowConfig workflowConfig, string output, ChatHistory chatHistory)
{
    /// <summary>
    /// The workflow configuration that was executed.
    /// </summary>
    public AgentWorkflowConfig WorkflowConfig { get; } = workflowConfig;

    /// <summary>
    /// The final output from the workflow.
    /// </summary>
    public string Output { get; } = output;

    /// <summary>
    /// The complete chat history from the workflow execution.
    /// </summary>
    public ChatHistory ChatHistory { get; } = chatHistory;
}
