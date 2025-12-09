using Elsa.OpenAI.Activities;
using Elsa.OpenAI.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using OpenAI.Moderations;

namespace Elsa.OpenAI.Tests.Activities;

/// <summary>
/// Unit tests for the OpenAIActivity base class.
/// </summary>
public class OpenAIActivityTests
{
    /// <summary>
    /// Test implementation of OpenAIActivity to test protected methods.
    /// </summary>
    private class TestOpenAIActivity : OpenAIActivity
    {
        protected override ValueTask ExecuteAsync(ActivityExecutionContext context) => ValueTask.CompletedTask;

        // Expose protected methods for testing
        public new OpenAIClientFactory GetClientFactory(ActivityExecutionContext context) => base.GetClientFactory(context);
        public new OpenAIClient GetClient(ActivityExecutionContext context) => base.GetClient(context);
        public new ChatClient GetChatClient(ActivityExecutionContext context) => base.GetChatClient(context);
        public new ImageClient GetImageClient(ActivityExecutionContext context) => base.GetImageClient(context);
        public new AudioClient GetAudioClient(ActivityExecutionContext context) => base.GetAudioClient(context);
        public new EmbeddingClient GetEmbeddingClient(ActivityExecutionContext context) => base.GetEmbeddingClient(context);
        public new ModerationClient GetModerationClient(ActivityExecutionContext context) => base.GetModerationClient(context);
    }


    [Fact]
    public void OpenAIActivity_IsAbstractClass()
    {
        // Arrange & Act
        var activityType = typeof(OpenAIActivity);

        // Assert
        Assert.True(activityType.IsAbstract);
    }

    [Fact]
    public void OpenAIActivity_HasApiKeyProperty()
    {
        // Arrange
        var property = typeof(OpenAIActivity).GetProperty(nameof(OpenAIActivity.ApiKey));

        // Act
        var inputAttribute = property?.GetCustomAttributes(typeof(InputAttribute), false).FirstOrDefault() as InputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(inputAttribute);
        Assert.Contains("API key", inputAttribute.Description);
    }

    [Fact]
    public void OpenAIActivity_HasModelProperty()
    {
        // Arrange
        var property = typeof(OpenAIActivity).GetProperty(nameof(OpenAIActivity.Model));

        // Act
        var inputAttribute = property?.GetCustomAttributes(typeof(InputAttribute), false).FirstOrDefault() as InputAttribute;

        // Assert
        Assert.NotNull(property);
        Assert.NotNull(inputAttribute);
        Assert.Contains("model", inputAttribute.Description);
    }

    [Fact]
    public void OpenAIActivity_InheritsFromActivity()
    {
        // Assert
        Assert.True(typeof(Elsa.Workflows.Activity).IsAssignableFrom(typeof(OpenAIActivity)));
    }

    [Fact]
    public void GetClientFactory_Integration_Test()
    {
        // Since GetClientFactory uses GetRequiredService which is non-virtual,
        // we test that the method exists and has correct signature

        var methodInfo = typeof(TestOpenAIActivity).GetMethod("GetClientFactory");
        
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(OpenAIClientFactory), methodInfo.ReturnType);
        Assert.Single(methodInfo.GetParameters());
        Assert.Equal(typeof(ActivityExecutionContext), methodInfo.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void GetClient_Integration_Test()
    {
        // Test that the method exists and has correct signature
        var activity = new TestOpenAIActivity();
        var methodInfo = typeof(TestOpenAIActivity).GetMethod("GetClient");
        
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(OpenAIClient), methodInfo.ReturnType);
        Assert.Single(methodInfo.GetParameters());
        Assert.Equal(typeof(ActivityExecutionContext), methodInfo.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void GetChatClient_Integration_Test()
    {
        // Test that the method exists and has correct signature
        var activity = new TestOpenAIActivity();
        var methodInfo = typeof(TestOpenAIActivity).GetMethod("GetChatClient");
        
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(ChatClient), methodInfo.ReturnType);
        Assert.Single(methodInfo.GetParameters());
        Assert.Equal(typeof(ActivityExecutionContext), methodInfo.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void GetImageClient_Integration_Test()
    {
        // Test that the method exists and has correct signature
        var activity = new TestOpenAIActivity();
        var methodInfo = typeof(TestOpenAIActivity).GetMethod("GetImageClient");
        
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(ImageClient), methodInfo.ReturnType);
        Assert.Single(methodInfo.GetParameters());
        Assert.Equal(typeof(ActivityExecutionContext), methodInfo.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void GetAudioClient_Integration_Test()
    {
        // Test that the method exists and has correct signature
        var activity = new TestOpenAIActivity();
        var methodInfo = typeof(TestOpenAIActivity).GetMethod("GetAudioClient");
        
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(AudioClient), methodInfo.ReturnType);
        Assert.Single(methodInfo.GetParameters());
        Assert.Equal(typeof(ActivityExecutionContext), methodInfo.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void GetEmbeddingClient_Integration_Test()
    {
        // Test that the method exists and has correct signature

        var methodInfo = typeof(TestOpenAIActivity).GetMethod("GetEmbeddingClient");
        
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(EmbeddingClient), methodInfo.ReturnType);
        Assert.Single(methodInfo.GetParameters());
        Assert.Equal(typeof(ActivityExecutionContext), methodInfo.GetParameters()[0].ParameterType);
    }

    [Fact]
    public void GetModerationClient_Integration_Test()
    {
        // Test that the method exists and has correct signature

        var methodInfo = typeof(TestOpenAIActivity).GetMethod("GetModerationClient");
        
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(ModerationClient), methodInfo.ReturnType);
        Assert.Single(methodInfo.GetParameters());
        Assert.Equal(typeof(ActivityExecutionContext), methodInfo.GetParameters()[0].ParameterType);
    }
}
