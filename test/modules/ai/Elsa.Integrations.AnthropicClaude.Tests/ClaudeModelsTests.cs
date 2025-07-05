using Elsa.Integrations.AnthropicClaude.Models;
using System.Text.Json;

namespace Elsa.Integrations.AnthropicClaude.Tests;

/// <summary>
/// Tests for Claude model serialization and deserialization.
/// </summary>
public class ClaudeModelsTests
{
    [Fact]
    public void ClaudeMessage_SerializesToJson_Correctly()
    {
        // Arrange
        var message = new ClaudeMessage
        {
            Role = "user",
            Content = "Hello, Claude!"
        };

        // Act
        var json = JsonSerializer.Serialize(message);

        // Assert
        Assert.Contains("\"role\":\"user\"", json);
        Assert.Contains("\"content\":\"Hello, Claude!\"", json);
    }

    [Fact]
    public void ClaudeCompletionRequest_SerializesToJson_WithSnakeCaseNaming()
    {
        // Arrange
        var request = new ClaudeCompletionRequest
        {
            Model = "claude-3-sonnet-20240229",
            MaxTokens = 1024,
            Messages = new List<ClaudeMessage>
            {
                new() { Role = "user", Content = "Test message" }
            },
            Temperature = 0.7
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Act
        var json = JsonSerializer.Serialize(request, options);

        // Assert
        Assert.Contains("\"max_tokens\":1024", json);
        Assert.Contains("\"model\":\"claude-3-sonnet-20240229\"", json);
        Assert.Contains("\"temperature\":0.7", json);
        Assert.Contains("\"messages\":", json);
    }

    [Fact]
    public void ClaudeCompletionResponse_DeserializesFromJson_Correctly()
    {
        // Arrange
        var json = """
        {
            "id": "msg_123",
            "type": "message",
            "role": "assistant",
            "content": [
                {
                    "type": "text",
                    "text": "Hello! How can I help you today?"
                }
            ],
            "model": "claude-3-sonnet-20240229",
            "stop_reason": "end_turn",
            "usage": {
                "input_tokens": 10,
                "output_tokens": 20
            }
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        // Act
        var response = JsonSerializer.Deserialize<ClaudeCompletionResponse>(json, options);

        // Assert
        Assert.NotNull(response);
        Assert.Equal("msg_123", response.Id);
        Assert.Equal("message", response.Type);
        Assert.Equal("assistant", response.Role);
        Assert.Equal("claude-3-sonnet-20240229", response.Model);
        Assert.Equal("end_turn", response.StopReason);
        
        Assert.NotNull(response.Content);
        Assert.Single(response.Content);
        Assert.Equal("text", response.Content[0].Type);
        Assert.Equal("Hello! How can I help you today?", response.Content[0].Text);
        
        Assert.NotNull(response.Usage);
        Assert.Equal(10, response.Usage.InputTokens);
        Assert.Equal(20, response.Usage.OutputTokens);
    }

    [Fact]
    public void ClaudeCompletionRequest_WithNullValues_SerializesCorrectly()
    {
        // Arrange
        var request = new ClaudeCompletionRequest
        {
            Model = "claude-3-haiku-20240307",
            MaxTokens = 512,
            Messages = new List<ClaudeMessage>
            {
                new() { Role = "user", Content = "Simple test" }
            },
            System = null,
            Temperature = null,
            StopSequences = null
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        // Act
        var json = JsonSerializer.Serialize(request, options);

        // Assert
        Assert.DoesNotContain("\"system\":", json);
        Assert.DoesNotContain("\"temperature\":", json);
        Assert.DoesNotContain("\"stop_sequences\":", json);
        Assert.Contains("\"model\":\"claude-3-haiku-20240307\"", json);
        Assert.Contains("\"max_tokens\":512", json);
    }
}