using Elsa.Integrations.AnthropicClaude.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Integrations.AnthropicClaude.Tests;

/// <summary>
/// Tests for Claude client factory functionality.
/// </summary>
public class ClaudeClientFactoryTests
{
    [Fact]
    public void GetClient_ReturnsConfiguredClient_WithApiKey()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var logger = Substitute.For<ILogger<ClaudeApiClient>>();
        var httpClient = new HttpClient();
        
        httpClientFactory.CreateClient("Claude").Returns(httpClient);
        
        var factory = new ClaudeClientFactory(httpClientFactory, logger);
        const string apiKey = "test-api-key";

        // Act
        var client = factory.GetClient(apiKey);

        // Assert
        Assert.NotNull(client);
        
        // Verify the HTTP client was configured with the API key
        Assert.Contains(httpClient.DefaultRequestHeaders, h => 
            h.Key == "x-api-key" && h.Value.Contains(apiKey));
    }

    [Fact]
    public void GetClient_CallsHttpClientFactory_WithCorrectName()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var logger = Substitute.For<ILogger<ClaudeApiClient>>();
        var httpClient = new HttpClient();
        
        httpClientFactory.CreateClient("Claude").Returns(httpClient);
        
        var factory = new ClaudeClientFactory(httpClientFactory, logger);

        // Act
        factory.GetClient("test-key");

        // Assert
        httpClientFactory.Received(1).CreateClient("Claude");
    }

    [Fact]
    public void GetClient_ReturnsNewInstance_EachTime()
    {
        // Arrange
        var httpClientFactory = Substitute.For<IHttpClientFactory>();
        var logger = Substitute.For<ILogger<ClaudeApiClient>>();
        
        httpClientFactory.CreateClient("Claude").Returns(new HttpClient(), new HttpClient());
        
        var factory = new ClaudeClientFactory(httpClientFactory, logger);

        // Act
        var client1 = factory.GetClient("test-key-1");
        var client2 = factory.GetClient("test-key-2");

        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        // Note: We can't directly compare instances since they wrap different HttpClient instances
        httpClientFactory.Received(2).CreateClient("Claude");
    }
}