using System.Collections.Concurrent;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using OpenAI.Moderations;

namespace Elsa.OpenAI.Services;

/// <summary>
/// Thread-safe factory for creating and caching OpenAI API clients.
/// Clients are cached by API key to avoid unnecessary allocations.
/// </summary>
public sealed class OpenAIClientFactory
{
    private readonly ConcurrentDictionary<string, OpenAIClient> _clients = new();

    /// <summary>
    /// Gets or creates a cached <see cref="OpenAIClient"/> for the specified API key.
    /// </summary>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <returns>A cached <see cref="OpenAIClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="apiKey"/> is null or whitespace.</exception>
    public OpenAIClient GetClient(string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return _clients.GetOrAdd(apiKey, static key => new OpenAIClient(key));
    }

    /// <summary>
    /// Gets a <see cref="ChatClient"/> for the specified model and API key.
    /// </summary>
    /// <param name="model">The model identifier (e.g., "gpt-4o").</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <returns>A <see cref="ChatClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="model"/> or <paramref name="apiKey"/> is null or whitespace.</exception>
    public ChatClient GetChatClient(string model, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        return GetClient(apiKey).GetChatClient(model);
    }

    /// <summary>
    /// Gets an <see cref="ImageClient"/> for the specified model and API key.
    /// </summary>
    /// <param name="model">The model identifier (e.g., "dall-e-3").</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <returns>An <see cref="ImageClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="model"/> or <paramref name="apiKey"/> is null or whitespace.</exception>
    public ImageClient GetImageClient(string model, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        return GetClient(apiKey).GetImageClient(model);
    }

    /// <summary>
    /// Gets an <see cref="AudioClient"/> for the specified model and API key.
    /// </summary>
    /// <param name="model">The model identifier (e.g., "whisper-1").</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <returns>An <see cref="AudioClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="model"/> or <paramref name="apiKey"/> is null or whitespace.</exception>
    public AudioClient GetAudioClient(string model, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        return GetClient(apiKey).GetAudioClient(model);
    }

    /// <summary>
    /// Gets an <see cref="EmbeddingClient"/> for the specified model and API key.
    /// </summary>
    /// <param name="model">The model identifier (e.g., "text-embedding-3-small").</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <returns>An <see cref="EmbeddingClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="model"/> or <paramref name="apiKey"/> is null or whitespace.</exception>
    public EmbeddingClient GetEmbeddingClient(string model, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        return GetClient(apiKey).GetEmbeddingClient(model);
    }

    /// <summary>
    /// Gets a <see cref="ModerationClient"/> for the specified model and API key.
    /// </summary>
    /// <param name="model">The model identifier (e.g., "omni-moderation-latest").</param>
    /// <param name="apiKey">The OpenAI API key.</param>
    /// <returns>A <see cref="ModerationClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="model"/> or <paramref name="apiKey"/> is null or whitespace.</exception>
    public ModerationClient GetModerationClient(string model, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        return GetClient(apiKey).GetModerationClient(model);
    }
}
