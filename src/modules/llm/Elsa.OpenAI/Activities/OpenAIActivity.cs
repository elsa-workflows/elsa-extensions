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
/// Abstract base class for all OpenAI workflow activities.
/// Provides shared inputs for API key and model, plus convenience methods
/// for resolving typed OpenAI clients from the <see cref="OpenAIClientFactory"/>.
/// </summary>
public abstract class OpenAIActivity : Activity
{
    /// <summary>
    /// The OpenAI API key used to authenticate requests.
    /// </summary>
    [Input(Description = "The OpenAI API key.")]
    public Input<string> ApiKey { get; set; } = null!;

    /// <summary>
    /// The OpenAI model identifier (e.g., "gpt-4o", "dall-e-3").
    /// </summary>
    [Input(Description = "The OpenAI model to use.")]
    public Input<string> Model { get; set; } = null!;

    /// <summary>
    /// Resolves the <see cref="OpenAIClientFactory"/> from the current execution context.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>The registered <see cref="OpenAIClientFactory"/> instance.</returns>
    protected static OpenAIClientFactory GetClientFactory(ActivityExecutionContext context) =>
        context.GetRequiredService<OpenAIClientFactory>();

    /// <summary>
    /// Gets a base <see cref="OpenAIClient"/> using the configured API key.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>An <see cref="OpenAIClient"/> instance.</returns>
    protected OpenAIClient GetClient(ActivityExecutionContext context) =>
        GetClientFactory(context).GetClient(context.Get(ApiKey)!);

    /// <summary>
    /// Gets a <see cref="ChatClient"/> using the configured model and API key.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>A <see cref="ChatClient"/> instance.</returns>
    protected ChatClient GetChatClient(ActivityExecutionContext context) =>
        GetClientFactory(context).GetChatClient(context.Get(Model)!, context.Get(ApiKey)!);

    /// <summary>
    /// Gets an <see cref="ImageClient"/> using the configured model and API key.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>An <see cref="ImageClient"/> instance.</returns>
    protected ImageClient GetImageClient(ActivityExecutionContext context) =>
        GetClientFactory(context).GetImageClient(context.Get(Model)!, context.Get(ApiKey)!);

    /// <summary>
    /// Gets an <see cref="AudioClient"/> using the configured model and API key.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>An <see cref="AudioClient"/> instance.</returns>
    protected AudioClient GetAudioClient(ActivityExecutionContext context) =>
        GetClientFactory(context).GetAudioClient(context.Get(Model)!, context.Get(ApiKey)!);

    /// <summary>
    /// Gets an <see cref="EmbeddingClient"/> using the configured model and API key.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>An <see cref="EmbeddingClient"/> instance.</returns>
    protected EmbeddingClient GetEmbeddingClient(ActivityExecutionContext context) =>
        GetClientFactory(context).GetEmbeddingClient(context.Get(Model)!, context.Get(ApiKey)!);

    /// <summary>
    /// Gets a <see cref="ModerationClient"/> using the configured model and API key.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>A <see cref="ModerationClient"/> instance.</returns>
    protected ModerationClient GetModerationClient(ActivityExecutionContext context) =>
        GetClientFactory(context).GetModerationClient(context.Get(Model)!, context.Get(ApiKey)!);
}
