using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0110

namespace Elsa.Agents;

/// <summary>
/// Executes multi-agent workflows using the Agent Framework.
/// </summary>
public class AgentWorkflowExecutor
{
    private readonly AgentFrameworkFactory _agentFactory;
    private readonly IKernelConfigProvider _kernelConfigProvider;

    public AgentWorkflowExecutor(
        AgentFrameworkFactory agentFactory,
        IKernelConfigProvider kernelConfigProvider)
    {
        _agentFactory = agentFactory;
        _kernelConfigProvider = kernelConfigProvider;
    }

    /// <summary>
    /// Executes an agent workflow and returns the result.
    /// </summary>
    public async Task<AgentWorkflowResult> ExecuteWorkflowAsync(
        string workflowName,
        AgentWorkflowConfig workflowConfig,
        IDictionary<string, object?> input,
        CancellationToken cancellationToken = default)
    {
        var kernelConfig = await _kernelConfigProvider.GetKernelConfigAsync(cancellationToken);
        
        // Create agents for the workflow
        var agents = new List<ChatCompletionAgent>();
        foreach (var agentName in workflowConfig.Agents)
        {
            if (!kernelConfig.Agents.TryGetValue(agentName, out var agentConfig))
                continue;
                
            var agent = _agentFactory.CreateAgent(kernelConfig, agentConfig);
            agents.Add(agent);
        }

        if (agents.Count == 0)
            throw new InvalidOperationException($"No agents found for workflow '{workflowName}'");

        // Create chat history with input
        ChatHistory chatHistory = [];
        
        // Format input as user message
        var inputMessage = FormatInput(input, workflowConfig);
        chatHistory.AddUserMessage(inputMessage);

        // Execute workflow based on type
        var result = workflowConfig.WorkflowType switch
        {
            AgentWorkflowType.Sequential => await ExecuteSequentialWorkflowAsync(agents, chatHistory, workflowConfig, cancellationToken),
            AgentWorkflowType.Graph => await ExecuteGraphWorkflowAsync(agents, chatHistory, workflowConfig, cancellationToken),
            _ => throw new NotSupportedException($"Workflow type {workflowConfig.WorkflowType} is not supported")
        };

        return new AgentWorkflowResult(workflowConfig, result, chatHistory);
    }

    private async Task<string> ExecuteSequentialWorkflowAsync(
        List<ChatCompletionAgent> agents,
        ChatHistory chatHistory,
        AgentWorkflowConfig config,
        CancellationToken cancellationToken)
    {
        var agentChat = new AgentGroupChat(agents.ToArray());
        
        // Configure termination
        ConfigureTermination(agentChat, config);

        // Execute chat until termination
        await foreach (var message in agentChat.InvokeAsync(cancellationToken))
        {
            chatHistory.Add(message);
        }

        // Return the last assistant message
        var lastMessage = chatHistory.LastOrDefault(m => m.Role == AuthorRole.Assistant);
        return lastMessage?.Content ?? string.Empty;
    }

    private async Task<string> ExecuteGraphWorkflowAsync(
        List<ChatCompletionAgent> agents,
        ChatHistory chatHistory,
        AgentWorkflowConfig config,
        CancellationToken cancellationToken)
    {
        // Create agent chat with selection strategy
        var agentChat = new AgentGroupChat(agents.ToArray());
        
        // Configure selection strategy if specified
        if (config.SelectionStrategy != null)
        {
            ConfigureSelectionStrategy(agentChat, config.SelectionStrategy, agents);
        }
        
        // Configure termination
        ConfigureTermination(agentChat, config);

        // Execute chat
        await foreach (var message in agentChat.InvokeAsync(cancellationToken))
        {
            chatHistory.Add(message);
        }

        var lastMessage = chatHistory.LastOrDefault(m => m.Role == AuthorRole.Assistant);
        return lastMessage?.Content ?? string.Empty;
    }

    private void ConfigureTermination(AgentGroupChat agentChat, AgentWorkflowConfig config)
    {
        // Configure termination based on the configuration
        switch (config.Termination.Type)
        {
            case TerminationType.MaxMessages:
                agentChat.ExecutionSettings = new AgentGroupChatSettings
                {
                    TerminationStrategy = new MaxChatHistoryTerminationStrategy(config.Termination.MaxMessages)
                };
                break;
            case TerminationType.Keyword:
                if (!string.IsNullOrEmpty(config.Termination.TerminationKeyword))
                {
                    agentChat.ExecutionSettings = new AgentGroupChatSettings
                    {
                        TerminationStrategy = new KeywordTerminationStrategy(config.Termination.TerminationKeyword)
                    };
                }
                break;
            // AgentDecision termination would require custom implementation
        }
    }

    private void ConfigureSelectionStrategy(
        AgentGroupChat agentChat,
        SelectionStrategyConfig strategyConfig,
        List<ChatCompletionAgent> agents)
    {
        // Configure agent selection based on strategy type
        // Note: The actual Agent Framework API may vary; this is a conceptual implementation
        switch (strategyConfig.Type)
        {
            case SelectionStrategyType.Sequential:
                agentChat.ExecutionSettings = new AgentGroupChatSettings
                {
                    SelectionStrategy = new SequentialSelectionStrategy()
                };
                break;
            case SelectionStrategyType.RoundRobin:
                // Round-robin is similar to sequential in most implementations
                agentChat.ExecutionSettings = new AgentGroupChatSettings
                {
                    SelectionStrategy = new SequentialSelectionStrategy()
                };
                break;
            // LLMBased and AgentBased would require custom strategies
        }
    }

    private string FormatInput(IDictionary<string, object?> input, AgentWorkflowConfig config)
    {
        if (input.Count == 0)
            return "Please begin the conversation.";

        var parts = input.Select(kvp => $"{kvp.Key}: {kvp.Value}");
        return string.Join("\n", parts);
    }
}

/// <summary>
/// Simple termination strategy based on maximum chat history length.
/// </summary>
internal class MaxChatHistoryTerminationStrategy : TerminationStrategy
{
    private readonly int _maxMessages;

    public MaxChatHistoryTerminationStrategy(int maxMessages)
    {
        _maxMessages = maxMessages;
    }

    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        // Terminate when history length reaches or exceeds the maximum
        return Task.FromResult(history.Count >= _maxMessages);
    }
}

/// <summary>
/// Termination strategy based on keyword detection.
/// </summary>
internal class KeywordTerminationStrategy : TerminationStrategy
{
    private readonly string _keyword;

    public KeywordTerminationStrategy(string keyword)
    {
        _keyword = keyword;
    }

    protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
    {
        var lastMessage = history.LastOrDefault();
        if (lastMessage?.Content == null)
            return Task.FromResult(false);

        return Task.FromResult(lastMessage.Content.Contains(_keyword, StringComparison.OrdinalIgnoreCase));
    }
}
