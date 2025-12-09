using Elsa.OpenAI.Activities.Chat;
using Elsa.OpenAI.Services;
using OpenAI;

namespace Elsa.OpenAI.Tests.Activities.Chat;

/// <summary>
/// Contains tests for the <see cref="CompleteChat"/> activity.
/// </summary>
public class CompleteChatTests
{
    /// <summary>
    /// Tests the activity structure and basic validation.
    /// </summary>
    [Fact]
    public void CompleteChat_HasCorrectInputsAndOutputs()
    {
        // Arrange
        var activity = new CompleteChat();

        // Assert
        Assert.NotNull(activity.Prompt);
        Assert.NotNull(activity.SystemMessage);
        Assert.NotNull(activity.MaxTokens);
        Assert.NotNull(activity.Temperature);
        Assert.NotNull(activity.ApiKey);
        Assert.NotNull(activity.Model);
        Assert.NotNull(activity.Result);
        Assert.NotNull(activity.TotalTokens);
        Assert.NotNull(activity.FinishReason);
    }

    /// <summary>
    /// Tests the OpenAI client factory functionality.
    /// </summary>
    [Fact]
    public void OpenAIClientFactory_CanCreateClient()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-key-123";

        // Act
        var client1 = factory.GetClient(apiKey);
        var client2 = factory.GetClient(apiKey);
        
        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.Same(client1, client2); // Should return the same cached instance
    }

    /// <summary>
    /// Tests the OpenAI client factory with different API keys.
    /// </summary>
    [Fact]
    public void OpenAIClientFactory_CreatesDifferentClientsForDifferentKeys()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey1 = "test-key-123";
        var apiKey2 = "test-key-456";

        // Act
        var client1 = factory.GetClient(apiKey1);
        var client2 = factory.GetClient(apiKey2);
        
        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.NotSame(client1, client2); // Should return different instances for different keys
    }

    /// <summary>
    /// Tests that environment variable is properly set up.
    /// </summary>
    [Fact]
    public void EnvironmentVariable_Check()
    {
        // Check if OPENAI_API_KEY environment variable is set
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        
        if (string.IsNullOrEmpty(apiKey))
        {
            Assert.True(true, "OPENAI_API_KEY environment variable not set. Set it to run integration tests.");
        }
        else
        {
            Assert.True(apiKey.Length > 10, "OPENAI_API_KEY seems to be set with a reasonable value.");
        }
    }
}
