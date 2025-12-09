using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;
using OpenAI.Chat;
using System.Text.Json;

namespace Elsa.OpenAI.Activities.Chat;

/// <summary>
/// Completes a chat conversation with tool/function calling support using OpenAI's Chat API.
/// </summary>
[Activity(
    "Elsa.OpenAI.Chat",
    "OpenAI Chat",
    "Completes a chat conversation with tool/function calling support using OpenAI's Chat API.",
    DisplayName = "Complete Chat With Tools")]
[UsedImplicitly]
public class CompleteChatWithTools : OpenAIActivity
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
    /// JSON array of tool definitions that the model can call.
    /// </summary>
    [Input(Description = "JSON array of tool definitions that the model can call.")]
    public Input<string?> ToolsJson { get; set; } = null!;

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
    public Output<string?> Result { get; set; } = null!;

    /// <summary>
    /// Tool calls made by the model (if any).
    /// </summary>
    [Output(Description = "Tool calls made by the model (if any).")]
    public Output<List<object>?> ToolCalls { get; set; } = null!;

    /// <summary>
    /// Whether the model requested tool calls.
    /// </summary>
    [Output(Description = "Whether the model requested tool calls.")]
    public Output<bool> HasToolCalls { get; set; } = null!;

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
        string? toolsJson = context.Get(ToolsJson);
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

        // Add tools if provided
        if (!string.IsNullOrWhiteSpace(toolsJson))
        {
            try
            {
                var toolsArray = JsonSerializer.Deserialize<JsonElement[]>(toolsJson);
                foreach (var toolElement in toolsArray)
                {
                    var tool = ChatTool.CreateFunctionTool(
                        toolElement.GetProperty("function").GetProperty("name").GetString()!,
                        toolElement.GetProperty("function").GetProperty("description").GetString(),
                        toolElement.GetProperty("function").TryGetProperty("parameters", out var parameters) ? 
                            BinaryData.FromString(parameters.GetRawText()) : null);
                    options.Tools.Add(tool);
                }
            }
            catch (JsonException)
            {
                // If tools JSON is invalid, continue without tools
            }
        }

        ChatCompletion completion = await client.CompleteChatAsync(messages, options);

        // Extract tool calls if any
        var toolCalls = new List<object>();
        bool hasToolCalls = false;

        if (completion.ToolCalls?.Count > 0)
        {
            hasToolCalls = true;
            foreach (var toolCall in completion.ToolCalls)
            {
                toolCalls.Add(new
                {
                    Id = toolCall.Id,
                    FunctionName = toolCall.FunctionName,
                    FunctionArguments = toolCall.FunctionArguments?.ToString()
                });
            }
        }

        context.Set(Result, completion.Content?.Count > 0 ? completion.Content[0].Text : null);
        context.Set(ToolCalls, toolCalls.Count > 0 ? toolCalls : null);
        context.Set(HasToolCalls, hasToolCalls);
        context.Set(FinishReason, completion.FinishReason?.ToString());
    }
}