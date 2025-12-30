using Elsa.OpenAI.Services;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI.Images;
using OpenAI.Moderations;

namespace Elsa.OpenAI.Tests.Services;

/// <summary>
/// Unit tests for the OpenAIClientFactory service.
/// </summary>
public class OpenAIClientFactoryTests
{
    [Fact]
    public void Constructor_CreatesInstance()
    {
        // Act
        var factory = new OpenAIClientFactory();

        // Assert
        Assert.NotNull(factory);
    }

    [Fact]
    public void GetClient_WithValidApiKey_ReturnsClient()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act
        var client = factory.GetClient(apiKey);

        // Assert
        Assert.NotNull(client);
        Assert.IsType<OpenAIClient>(client);
    }

    [Fact]
    public void GetClient_WithSameApiKey_ReturnsSameInstance()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act
        var client1 = factory.GetClient(apiKey);
        var client2 = factory.GetClient(apiKey);

        // Assert
        Assert.Same(client1, client2);
    }

    [Fact]
    public void GetClient_WithDifferentApiKeys_ReturnsDifferentInstances()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey1 = "test-api-key-1";
        var apiKey2 = "test-api-key-2";

        // Act
        var client1 = factory.GetClient(apiKey1);
        var client2 = factory.GetClient(apiKey2);

        // Assert
        Assert.NotSame(client1, client2);
    }

    [Fact]
    public void GetClient_WithNullApiKey_ThrowsException()
    {
        // Arrange
        var factory = new OpenAIClientFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.GetClient(null!));
    }

    [Fact]
    public void GetClient_WithEmptyApiKey_ThrowsException()
    {
        // Arrange
        var factory = new OpenAIClientFactory();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.GetClient(string.Empty));
    }

    [Fact]
    public void GetChatClient_WithValidParameters_ReturnsChatClient()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var model = "gpt-3.5-turbo";
        var apiKey = "test-api-key";

        // Act
        var chatClient = factory.GetChatClient(model, apiKey);

        // Assert
        Assert.NotNull(chatClient);
        Assert.IsType<ChatClient>(chatClient);
    }

    [Fact]
    public void GetChatClient_WithNullModel_ThrowsException()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.GetChatClient(null!, apiKey));
    }

    [Fact]
    public void GetChatClient_WithEmptyModel_ThrowsException()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => factory.GetChatClient(string.Empty, apiKey));
    }

    [Fact]
    public void GetImageClient_WithValidParameters_ReturnsImageClient()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var model = "dall-e-3";
        var apiKey = "test-api-key";

        // Act
        var imageClient = factory.GetImageClient(model, apiKey);

        // Assert
        Assert.NotNull(imageClient);
        Assert.IsType<ImageClient>(imageClient);
    }

    [Fact]
    public void GetImageClient_WithNullModel_ThrowsException()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.GetImageClient(null!, apiKey));
    }

    [Fact]
    public void GetAudioClient_WithValidParameters_ReturnsAudioClient()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var model = "whisper-1";
        var apiKey = "test-api-key";

        // Act
        var audioClient = factory.GetAudioClient(model, apiKey);

        // Assert
        Assert.NotNull(audioClient);
        Assert.IsType<AudioClient>(audioClient);
    }

    [Fact]
    public void GetAudioClient_WithNullModel_ThrowsException()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.GetAudioClient(null!, apiKey));
    }

    [Fact]
    public void GetEmbeddingClient_WithValidParameters_ReturnsEmbeddingClient()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var model = "text-embedding-3-small";
        var apiKey = "test-api-key";

        // Act
        var embeddingClient = factory.GetEmbeddingClient(model, apiKey);

        // Assert
        Assert.NotNull(embeddingClient);
        Assert.IsType<EmbeddingClient>(embeddingClient);
    }

    [Fact]
    public void GetEmbeddingClient_WithNullModel_ThrowsException()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.GetEmbeddingClient(null!, apiKey));
    }

    [Fact]
    public void GetModerationClient_WithValidParameters_ReturnsModerationClient()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var model = "omni-moderation-latest";
        var apiKey = "test-api-key";

        // Act
        var moderationClient = factory.GetModerationClient(model, apiKey);

        // Assert
        Assert.NotNull(moderationClient);
        Assert.IsType<ModerationClient>(moderationClient);
    }

    [Fact]
    public void GetModerationClient_WithNullModel_ThrowsException()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.GetModerationClient(null!, apiKey));
    }

    [Fact]
    public void MultipleClientTypes_WithSameApiKey_ReuseBaseClient()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";

        // Act
        var baseClient1 = factory.GetClient(apiKey);
        var chatClient = factory.GetChatClient("gpt-3.5-turbo", apiKey);
        var baseClient2 = factory.GetClient(apiKey);

        // Assert
        Assert.Same(baseClient1, baseClient2);
        Assert.NotNull(chatClient);
    }

    [Fact]
    public void ConcurrentAccess_WithSameApiKey_ThreadSafe()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var apiKey = "test-api-key";
        var clients = new OpenAIClient[10];

        // Act
        Parallel.For(0, 10, i =>
        {
            clients[i] = factory.GetClient(apiKey);
        });

        // Assert
        Assert.All(clients, client => Assert.NotNull(client));
        Assert.All(clients, client => Assert.Same(clients[0], client));
    }

    [Fact]
    public void ConcurrentAccess_WithDifferentApiKeys_ThreadSafe()
    {
        // Arrange
        var factory = new OpenAIClientFactory();
        var clients = new OpenAIClient[10];

        // Act
        Parallel.For(0, 10, i =>
        {
            clients[i] = factory.GetClient($"test-api-key-{i}");
        });

        // Assert
        Assert.All(clients, client => Assert.NotNull(client));
        
        // Verify all clients are different
        for (int i = 0; i < 10; i++)
        {
            for (int j = i + 1; j < 10; j++)
            {
                Assert.NotSame(clients[i], clients[j]);
            }
        }
    }
}