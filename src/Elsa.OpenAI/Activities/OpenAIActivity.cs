using Elsa.OpenAI.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using OpenAI.Moderations;

namespace Elsa.OpenAI.Activities;

/// <summary>
/// Generic base class inherited by all OpenAI activities.
/// </summary>
public abstract class OpenAIActivity : Activity
{
    /// <summary>
    /// The OpenAI API key.
    /// </summary>
    [Input(Description = "The OpenAI API key.")]
    public Input<string> ApiKey { get; set; } = null!;

    /// <summary>
    /// The OpenAI model to use.
    /// </summary>
    [Input(Description = "The OpenAI model to use.")]
    public Input<string> Model { get; set; } = null!;

    /// <summary>
    /// Gets the OpenAI client factory.
    /// </summary>
    /// <param name="context">The current context to get the factory.</param>
    /// <returns>The OpenAI client factory.</returns>
    protected OpenAIClientFactory GetClientFactory(ActivityExecutionContext context) => 
        context.GetRequiredService<OpenAIClientFactory>();

    /// <summary>
    /// Gets the OpenAI client.
    /// </summary>
    /// <param name="context">The current context to get the client.</param>
    /// <returns>The OpenAI client.</returns>
    protected OpenAIClient GetClient(ActivityExecutionContext context)
    {
        OpenAIClientFactory clientFactory = GetClientFactory(context);
        string apiKey = context.Get(ApiKey)!;
        return clientFactory.GetClient(apiKey);
    }

    /// <summary>
    /// Gets the ChatClient for the specified model.
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <returns>The ChatClient.</returns>
    protected ChatClient GetChatClient(ActivityExecutionContext context)
    {
        OpenAIClientFactory clientFactory = GetClientFactory(context);
        string model = context.Get(Model)!;
        string apiKey = context.Get(ApiKey)!;
        return clientFactory.GetChatClient(model, apiKey);
    }

    /// <summary>
    /// Gets the ImageClient for the specified model.
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <returns>The ImageClient.</returns>
    protected ImageClient GetImageClient(ActivityExecutionContext context)
    {
        OpenAIClientFactory clientFactory = GetClientFactory(context);
        string model = context.Get(Model)!;
        string apiKey = context.Get(ApiKey)!;
        return clientFactory.GetImageClient(model, apiKey);
    }

    /// <summary>
    /// Gets the AudioClient for the specified model.
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <returns>The AudioClient.</returns>
    protected AudioClient GetAudioClient(ActivityExecutionContext context)
    {
        OpenAIClientFactory clientFactory = GetClientFactory(context);
        string model = context.Get(Model)!;
        string apiKey = context.Get(ApiKey)!;
        return clientFactory.GetAudioClient(model, apiKey);
    }

    /// <summary>
    /// Gets the EmbeddingClient for the specified model.
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <returns>The EmbeddingClient.</returns>
    protected EmbeddingClient GetEmbeddingClient(ActivityExecutionContext context)
    {
        OpenAIClientFactory clientFactory = GetClientFactory(context);
        string model = context.Get(Model)!;
        string apiKey = context.Get(ApiKey)!;
        return clientFactory.GetEmbeddingClient(model, apiKey);
    }

    /// <summary>
    /// Gets the ModerationClient for the specified model.
    /// </summary>
    /// <param name="context">The current context.</param>
    /// <returns>The ModerationClient.</returns>
    protected ModerationClient GetModerationClient(ActivityExecutionContext context)
    {
        OpenAIClientFactory clientFactory = GetClientFactory(context);
        string model = context.Get(Model)!;
        string apiKey = context.Get(ApiKey)!;
        return clientFactory.GetModerationClient(model, apiKey);
    }
}