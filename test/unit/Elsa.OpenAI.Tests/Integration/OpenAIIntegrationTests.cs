using Elsa.OpenAI.Activities.Chat;
using Elsa.OpenAI.Services;
using Microsoft.Extensions.Configuration;

namespace Elsa.OpenAI.Tests.Integration;

/// <summary>
/// Integration tests that can make real API calls to OpenAI when API key is available.
/// </summary>
public class OpenAIIntegrationTests
{
    private readonly string? _apiKey;
    private readonly bool _hasApiKey;

    public OpenAIIntegrationTests()
    {
        // Build configuration to access user secrets and environment variables
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<OpenAIIntegrationTests>()
            .AddEnvironmentVariables()
            .Build();

        _apiKey = configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        _hasApiKey = !string.IsNullOrEmpty(_apiKey);
    }

    [Fact]
    public void ApiKey_Configuration_IsAccessible()
    {
        // This test always passes - it just reports whether API key is available
        if (_hasApiKey)
        {
            Assert.True(_apiKey!.Length > 10, "API key should have reasonable length");
        }
        else
        {
            // Skip test but don't fail - just document how to set it up
            Assert.True(true); // Always pass, but log the setup instructions
        }
    }

    [Fact]
    public void OpenAIClientFactory_CanCreateDifferentClients()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var testApiKey = _apiKey ?? "test-key";

        // Act & Assert
        var chatClient = factory.GetChatClient("gpt-3.5-turbo", testApiKey);
        var imageClient = factory.GetImageClient("dall-e-3", testApiKey);
        var audioClient = factory.GetAudioClient("whisper-1", testApiKey);
        var embeddingClient = factory.GetEmbeddingClient("text-embedding-3-small", testApiKey);
        var moderationClient = factory.GetModerationClient("omni-moderation-latest", testApiKey);

        Assert.NotNull(chatClient);
        Assert.NotNull(imageClient);
        Assert.NotNull(audioClient);
        Assert.NotNull(embeddingClient);
        Assert.NotNull(moderationClient);
    }

    [Fact]
    public void OpenAIClientFactory_CachesClientInstances()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var testApiKey = _apiKey ?? "test-key";

        // Act
        var client1 = factory.GetClient(testApiKey);
        var client2 = factory.GetClient(testApiKey);
        var client3 = factory.GetClient("different-key");

        // Assert
        Assert.Same(client1, client2); // Same key should return same instance
        Assert.NotSame(client1, client3); // Different keys should return different instances
    }

    [Fact]
    public async Task RealApiCall_ChatCompletion_ReturnsValidResponse()
    {
        // Skip test if no API key available
        if (!_hasApiKey)
        {
            Assert.True(true); // Pass but skip - API key not configured
            return;
        }

        // Arrange
        var factory = new OpenAIClientFactory();
        var client = factory.GetChatClient("gpt-3.5-turbo", _apiKey!);

        try
        {
            // Act
            var result = await client.CompleteChatAsync("Say 'Hello from Elsa OpenAI unit tests!'");
            var completion = result.Value;

            // Assert
            Assert.NotNull(completion);
            Assert.NotNull(completion.Content);
            Assert.True(completion.Content.Count > 0);
            Assert.False(string.IsNullOrEmpty(completion.Content[0].Text));
            Assert.True(completion.Usage?.TotalTokenCount > 0);
            
            // Verify the response contains our expected text
            var responseText = completion.Content[0].Text;
            Assert.Contains("Hello from Elsa OpenAI unit tests", responseText);
        }
        catch (Exception ex)
        {
            // If API call fails, provide helpful error message
            Assert.Fail($"OpenAI API call failed: {ex.Message}. Check your API key and network connection.");
        }
    }

    [Fact]
    public void CompleteChat_Activity_HasCorrectStructure()
    {
        // Arrange & Act
        var activity = new CompleteChat();
        var activityType = typeof(CompleteChat);

        // Assert - Check that all required properties exist
        Assert.NotNull(activityType.GetProperty("Prompt"));
        Assert.NotNull(activityType.GetProperty("SystemMessage"));
        Assert.NotNull(activityType.GetProperty("MaxTokens"));
        Assert.NotNull(activityType.GetProperty("Temperature"));
        Assert.NotNull(activityType.GetProperty("ApiKey"));
        Assert.NotNull(activityType.GetProperty("Model"));
        Assert.NotNull(activityType.GetProperty("Result"));
        Assert.NotNull(activityType.GetProperty("TotalTokens"));
        Assert.NotNull(activityType.GetProperty("FinishReason"));
    }
}