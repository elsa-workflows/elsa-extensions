using System.Text.Json.Serialization;

namespace Elsa.Integrations.AnthropicClaude.Models;

/// <summary>
/// Represents a message in a Claude conversation.
/// </summary>
public class ClaudeMessage
{
    /// <summary>
    /// The role of the message sender (user, assistant, or system).
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    /// <summary>
    /// The content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = null!;
}

/// <summary>
/// Represents a request to the Claude API for creating completions.
/// </summary>
public class ClaudeCompletionRequest
{
    /// <summary>
    /// The model to use for the completion.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "claude-3-sonnet-20240229";

    /// <summary>
    /// Maximum number of tokens to generate.
    /// </summary>
    [JsonPropertyName("max_tokens")]
    public int MaxTokens { get; set; } = 1024;

    /// <summary>
    /// The messages to send to Claude.
    /// </summary>
    [JsonPropertyName("messages")]
    public List<ClaudeMessage> Messages { get; set; } = new();

    /// <summary>
    /// System prompt for the conversation.
    /// </summary>
    [JsonPropertyName("system")]
    public string? System { get; set; }

    /// <summary>
    /// Temperature for response randomness (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    /// Stop sequences to end generation.
    /// </summary>
    [JsonPropertyName("stop_sequences")]
    public List<string>? StopSequences { get; set; }
}

/// <summary>
/// Represents the content of a Claude API response.
/// </summary>
public class ClaudeResponseContent
{
    /// <summary>
    /// The type of content (usually "text").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// The text content of the response.
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}

/// <summary>
/// Represents a response from the Claude API.
/// </summary>
public class ClaudeCompletionResponse
{
    /// <summary>
    /// The unique identifier for this response.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// The type of object returned.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    /// <summary>
    /// The role of the responder (should be "assistant").
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = null!;

    /// <summary>
    /// The content of the response.
    /// </summary>
    [JsonPropertyName("content")]
    public List<ClaudeResponseContent> Content { get; set; } = new();

    /// <summary>
    /// The model that generated this response.
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = null!;

    /// <summary>
    /// The reason the response ended.
    /// </summary>
    [JsonPropertyName("stop_reason")]
    public string? StopReason { get; set; }

    /// <summary>
    /// The stop sequence that ended the response, if any.
    /// </summary>
    [JsonPropertyName("stop_sequence")]
    public string? StopSequence { get; set; }

    /// <summary>
    /// Usage statistics for this response.
    /// </summary>
    [JsonPropertyName("usage")]
    public ClaudeUsage? Usage { get; set; }
}

/// <summary>
/// Represents usage statistics from a Claude API response.
/// </summary>
public class ClaudeUsage
{
    /// <summary>
    /// Number of input tokens used.
    /// </summary>
    [JsonPropertyName("input_tokens")]
    public int InputTokens { get; set; }

    /// <summary>
    /// Number of output tokens generated.
    /// </summary>
    [JsonPropertyName("output_tokens")]
    public int OutputTokens { get; set; }
}