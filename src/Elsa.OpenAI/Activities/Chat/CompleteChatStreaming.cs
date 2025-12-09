using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Chat;
using System.Text;

namespace Elsa.OpenAI.Activities.Chat;

/// <summary>
/// Completes a chat conversation with streaming using OpenAI's Chat API.
/// </summary>
[Activity(
    "Elsa.OpenAI.Chat",
    "OpenAI Chat",
    "Completes a chat conversation with streaming using OpenAI's Chat API.",
    DisplayName = "Complete Chat Streaming")]
[UsedImplicitly]
public class CompleteChatStreaming : OpenAIActivity
{
    /// <summary>
    /// The user message or prompt to complete.
    /// </summary>
    [Input(Description = "The user message or prompt to complete.")]
    public Input<string> Prompt { get; set; } = null!;

    /// <summary>
    /// Optional system message to provide context or instructions.
    /// </summary>
    [Input(Description = "Optional system message to provide context or instructions.")]
    public Input<string?> SystemMessage { get; set; } = null!;

    /// <summary>
    /// The maximum number of tokens to generate.
    /// </summary>
    [Input(Description = "The maximum number of tokens to generate.")]
    public Input<int?> MaxTokens { get; set; } = null!;

    /// <summary>
    /// Controls randomness: 0.0 is deterministic, 1.0 is maximum randomness.
    /// </summary>
    [Input(Description = "Controls randomness: 0.0 is deterministic, 1.0 is maximum randomness.")]
    public Input<float?> Temperature { get; set; } = null!;

    /// <summary>
    /// The complete result from the streaming chat model.
    /// </summary>
    [Output(Description = "The complete result from the streaming chat model.")]
    public Output<string> Result { get; set; } = null!;

    /// <summary>
    /// The list of streaming updates received during completion.
    /// </summary>
    [Output(Description = "The list of streaming updates received during completion.")]
    public Output<List<string>> StreamingUpdates { get; set; } = null!;

    /// <summary>
    /// The finish reason for the completion.
    /// </summary>
    [Output(Description = "The finish reason for the completion.")]
    public Output<string?> FinishReason { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        string prompt = context.Get(Prompt)!;
        string? systemMessage = context.Get(SystemMessage);
        int? maxTokens = context.Get(MaxTokens);
        float? temperature = context.Get(Temperature);

        ChatClient client = GetChatClient(context);

        // Build the messages list
        var messages = new List<ChatMessage>();
        
        if (!string.IsNullOrWhiteSpace(systemMessage))
        {
            messages.Add(ChatMessage.CreateSystemMessage(systemMessage));
        }
        
        messages.Add(ChatMessage.CreateUserMessage(prompt));

        // Create completion options
        var options = new ChatCompletionOptions();
        if (maxTokens.HasValue)
            options.MaxOutputTokenCount = maxTokens.Value;
        if (temperature.HasValue)
            options.Temperature = temperature.Value;

        var completionUpdates = client.CompleteChatStreamingAsync(messages, options);
        
        var result = new StringBuilder();
        var updates = new List<string>();
        string? finishReason = null;

        await foreach (StreamingChatCompletionUpdate completionUpdate in completionUpdates)
        {
            if (completionUpdate.ContentUpdate.Count > 0)
            {
                string updateText = completionUpdate.ContentUpdate[0].Text;
                if (!string.IsNullOrEmpty(updateText))
                {
                    result.Append(updateText);
                    updates.Add(updateText);
                }
            }

            if (completionUpdate.FinishReason != null)
            {
                finishReason = completionUpdate.FinishReason.ToString();
            }
        }

        context.Set(Result, result.ToString());
        context.Set(StreamingUpdates, updates);
        context.Set(FinishReason, finishReason);
    }
}