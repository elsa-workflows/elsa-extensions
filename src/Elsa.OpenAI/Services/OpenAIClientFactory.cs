using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using OpenAI.Moderations;

namespace Elsa.OpenAI.Services;

/// <summary>
/// Factory for creating OpenAI API clients.
/// </summary>
public class OpenAIClientFactory
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Dictionary<string, OpenAIClient> _openAIClients = new();

    /// <summary>
    /// Gets an OpenAI client for the specified API key.
    /// </summary>
    public OpenAIClient GetClient(string apiKey)
    {
        if (_openAIClients.TryGetValue(apiKey, out OpenAIClient? client))
            return client;

        try
        {
            _semaphore.Wait();

            if (_openAIClients.TryGetValue(apiKey, out client))
                return client;

            OpenAIClient newClient = new(apiKey);
            _openAIClients[apiKey] = newClient;
            return newClient;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Gets a ChatClient for the specified model and API key.
    /// </summary>
    public ChatClient GetChatClient(string model, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model must not be null or empty.", nameof(model));
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key must not be null or empty.", nameof(apiKey));
        OpenAIClient client = GetClient(apiKey);
        return client.GetChatClient(model);
    }

    /// <summary>
    /// Gets an ImageClient for the specified model and API key.
    /// </summary>
    public ImageClient GetImageClient(string model, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model must not be null or empty.", nameof(model));
        OpenAIClient client = GetClient(apiKey);
        return client.GetImageClient(model);
    }

    /// <summary>
    /// Gets an AudioClient for the specified model and API key.
    /// </summary>
    public AudioClient GetAudioClient(string model, string apiKey)
    {
        OpenAIClient client = GetClient(apiKey);
        return client.GetAudioClient(model);
    }

    /// <summary>
    /// Gets an EmbeddingClient for the specified model and API key.
    /// </summary>
    public EmbeddingClient GetEmbeddingClient(string model, string apiKey)
    {
        OpenAIClient client = GetClient(apiKey);
        return client.GetEmbeddingClient(model);
    }

    /// <summary>
    /// Gets a ModerationClient for the specified model and API key.
    /// </summary>
    public ModerationClient GetModerationClient(string model, string apiKey)
    {
        OpenAIClient client = GetClient(apiKey);
        return client.GetModerationClient(model);
    }
}