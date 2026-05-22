using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Chat;

namespace Elsa.OpenAI.Activities.Chat;

/// <summary>
/// Completes a chat conversation using OpenAI's Chat API.
/// Supports an optional system message, max token limit, and temperature control.
/// </summary>
[Activity(
    "Elsa.OpenAI.Chat",
    "OpenAI Chat",
    "Completes a chat conversation using OpenAI's Chat API.",
    DisplayName = "Complete Chat")]
[UsedImplicitly]
public class CompleteChat : OpenAIActivity
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
    /// The completion result from the chat model.
    /// </summary>
    [Output(Description = "The completion result from the chat model.")]
    public Output<string> Result { get; set; } = null!;

    /// <summary>
    /// The total tokens used in the request.
    /// </summary>
    [Output(Description = "The total tokens used in the request.")]
    public Output<int?> TotalTokens { get; set; } = null!;

    /// <summary>
    /// The finish reason for the completion.
    /// </summary>
    [Output(Description = "The finish reason for the completion.")]
    public Output<string?> FinishReason { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var prompt = context.Get(Prompt)!;
        var systemMessage = context.Get(SystemMessage);
        var maxTokens = context.Get(MaxTokens);
        var temperature = context.Get(Temperature);

        var client = GetChatClient(context);

        var messages = new List<ChatMessage>();

        if (!string.IsNullOrWhiteSpace(systemMessage))
            messages.Add(ChatMessage.CreateSystemMessage(systemMessage));

        messages.Add(ChatMessage.CreateUserMessage(prompt));

        var options = new ChatCompletionOptions();

        if (maxTokens.HasValue)
            options.MaxOutputTokenCount = maxTokens.Value;

        if (temperature.HasValue)
            options.Temperature = temperature.Value;

        var completion = await client.CompleteChatAsync(messages, options);

        context.Set(Result, completion.Value.Content[0]?.Text ?? string.Empty);
        context.Set(TotalTokens, completion.Value.Usage?.TotalTokenCount);
        context.Set(FinishReason, completion.Value.FinishReason.ToString());
    }
}
