using Elsa.OpenAI.Activities;
using Elsa.OpenAI.Activities.Chat;
using Elsa.OpenAI.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Moq;
using OpenAI;
using OpenAI.Chat;

namespace Elsa.OpenAI.Tests.Activities.Chat;

/// <summary>
/// Contains tests for the <see cref="CompleteChat"/> activity.
/// </summary>
public class CompleteChatTests
{
    /// <summary>
    /// Test implementation of CompleteChat to expose protected methods.
    /// </summary>
    private class TestableCompleteChat : CompleteChat
    {
        public new async ValueTask ExecuteAsync(ActivityExecutionContext context) => await base.ExecuteAsync(context);
    }
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var activity = new CompleteChat();

        // Assert
        Assert.NotNull(activity);
    }

    [Fact]
    public void CompleteChat_HasCorrectInputProperties()
    {
        // Arrange & Act - Test that properties exist and have correct types
        var activityType = typeof(CompleteChat);
        var promptProperty = activityType.GetProperty(nameof(CompleteChat.Prompt));
        var systemMessageProperty = activityType.GetProperty(nameof(CompleteChat.SystemMessage));
        var maxTokensProperty = activityType.GetProperty(nameof(CompleteChat.MaxTokens));
        var temperatureProperty = activityType.GetProperty(nameof(CompleteChat.Temperature));
        var apiKeyProperty = activityType.GetProperty(nameof(CompleteChat.ApiKey));
        var modelProperty = activityType.GetProperty(nameof(CompleteChat.Model));

        // Assert
        Assert.NotNull(promptProperty);
        Assert.NotNull(systemMessageProperty);
        Assert.NotNull(maxTokensProperty);
        Assert.NotNull(temperatureProperty);
        Assert.NotNull(apiKeyProperty);
        Assert.NotNull(modelProperty);
        
        // Verify property types
        Assert.Equal(typeof(Input<string>), promptProperty.PropertyType);
        Assert.Equal(typeof(Input<string?>), systemMessageProperty.PropertyType);
        Assert.Equal(typeof(Input<int?>), maxTokensProperty.PropertyType);
        Assert.Equal(typeof(Input<float?>), temperatureProperty.PropertyType);
        Assert.Equal(typeof(Input<string>), apiKeyProperty.PropertyType);
        Assert.Equal(typeof(Input<string>), modelProperty.PropertyType);
    }

    [Fact]
    public void CompleteChat_HasCorrectOutputProperties()
    {
        // Arrange & Act - Test that properties exist and have correct types
        var activityType = typeof(CompleteChat);
        var resultProperty = activityType.GetProperty(nameof(CompleteChat.Result));
        var totalTokensProperty = activityType.GetProperty(nameof(CompleteChat.TotalTokens));
        var finishReasonProperty = activityType.GetProperty(nameof(CompleteChat.FinishReason));

        // Assert
        Assert.NotNull(resultProperty);
        Assert.NotNull(totalTokensProperty);
        Assert.NotNull(finishReasonProperty);
        
        // Verify property types
        Assert.Equal(typeof(Output<string>), resultProperty.PropertyType);
        Assert.Equal(typeof(Output<int?>), totalTokensProperty.PropertyType);
        Assert.Equal(typeof(Output<string?>), finishReasonProperty.PropertyType);
    }

    [Fact]
    public void CompleteChat_HasActivityAttribute()
    {
        // Arrange
        var activityType = typeof(CompleteChat);

        // Act
        var activityAttribute = activityType.GetCustomAttributes(typeof(ActivityAttribute), false).FirstOrDefault() as ActivityAttribute;

        // Assert
        Assert.NotNull(activityAttribute);
        Assert.Equal("Elsa.OpenAI.Chat", activityAttribute.Namespace);
        Assert.Equal("OpenAI Chat", activityAttribute.Category);
        Assert.Equal("Complete Chat", activityAttribute.DisplayName);
        Assert.Contains("chat conversation", activityAttribute.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Prompt_HasInputAttribute()
    {
        // Arrange
        var property = typeof(CompleteChat).GetProperty(nameof(CompleteChat.Prompt));

        // Act
        var inputAttribute = property?.GetCustomAttributes(typeof(InputAttribute), false).FirstOrDefault() as InputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(inputAttribute);
        Assert.Contains("prompt", inputAttribute.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SystemMessage_HasInputAttribute()
    {
        // Arrange
        var property = typeof(CompleteChat).GetProperty(nameof(CompleteChat.SystemMessage));

        // Act
        var inputAttribute = property?.GetCustomAttributes(typeof(InputAttribute), false).FirstOrDefault() as InputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(inputAttribute);
        Assert.Contains("system message", inputAttribute.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MaxTokens_HasInputAttribute()
    {
        // Arrange
        var property = typeof(CompleteChat).GetProperty(nameof(CompleteChat.MaxTokens));

        // Act
        var inputAttribute = property?.GetCustomAttributes(typeof(InputAttribute), false).FirstOrDefault() as InputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(inputAttribute);
        Assert.Contains("tokens", inputAttribute.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Temperature_HasInputAttribute()
    {
        // Arrange
        var property = typeof(CompleteChat).GetProperty(nameof(CompleteChat.Temperature));

        // Act
        var inputAttribute = property?.GetCustomAttributes(typeof(InputAttribute), false).FirstOrDefault() as InputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(inputAttribute);
        Assert.Contains("randomness", inputAttribute.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Result_HasOutputAttribute()
    {
        // Arrange
        var property = typeof(CompleteChat).GetProperty(nameof(CompleteChat.Result));

        // Act
        var outputAttribute = property?.GetCustomAttributes(typeof(OutputAttribute), false).FirstOrDefault() as OutputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(outputAttribute);
        Assert.Contains("result", outputAttribute.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TotalTokens_HasOutputAttribute()
    {
        // Arrange
        var property = typeof(CompleteChat).GetProperty(nameof(CompleteChat.TotalTokens));

        // Act
        var outputAttribute = property?.GetCustomAttributes(typeof(OutputAttribute), false).FirstOrDefault() as OutputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(outputAttribute);
        Assert.Contains("tokens", outputAttribute.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FinishReason_HasOutputAttribute()
    {
        // Arrange
        var property = typeof(CompleteChat).GetProperty(nameof(CompleteChat.FinishReason));

        // Act
        var outputAttribute = property?.GetCustomAttributes(typeof(OutputAttribute), false).FirstOrDefault() as OutputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(outputAttribute);
        Assert.Contains("finish reason", outputAttribute.Description, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public void CompleteChat_HasCorrectAttributes()
    {
        // Arrange
        var activityType = typeof(CompleteChat);

        // Act - Check for Activity attribute (which we know exists)
        var activityAttribute = activityType.GetCustomAttributes(typeof(ActivityAttribute), false).FirstOrDefault();
        var allAttributes = activityType.GetCustomAttributes(false);

        // Assert
        Assert.NotNull(activityAttribute);
        Assert.True(allAttributes.Length > 0, "CompleteChat should have at least one attribute");
    }

    [Fact]
    public void ExecuteAsync_MethodExists_AndIsProtected()
    {
        // Test that ExecuteAsync method exists and has the correct signature
        var activityType = typeof(CompleteChat);
        var executeMethod = activityType.GetMethod("ExecuteAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(executeMethod);
        Assert.Equal(typeof(ValueTask), executeMethod.ReturnType);
        Assert.Single(executeMethod.GetParameters());
        Assert.Equal(typeof(ActivityExecutionContext), executeMethod.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void CompleteChat_InheritsFromOpenAIActivity()
    {
        // Verify inheritance structure
        Assert.True(typeof(OpenAIActivity).IsAssignableFrom(typeof(CompleteChat)));
    }

    [Fact]
    public void CompleteChat_UsesGetChatClientMethod()
    {
        // This test verifies that CompleteChat has access to the GetChatClient method from base class
        var activity = new CompleteChat();
        var baseType = typeof(OpenAIActivity);
        var getChatClientMethod = baseType.GetMethod("GetChatClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        Assert.NotNull(getChatClientMethod);
        Assert.Equal(typeof(ChatClient), getChatClientMethod.ReturnType);
    }
}
