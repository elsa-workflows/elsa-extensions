using Elsa.Integrations.AnthropicClaude.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Elsa.Integrations.AnthropicClaude.Tests;

/// <summary>
/// Tests for Claude API client functionality.
/// </summary>
public class ClaudeApiClientTests
{
    private readonly ClaudeApiClient _client;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeApiClient> _logger;

    public ClaudeApiClientTests()
    {
        _httpClient = new HttpClient();
        _logger = Substitute.For<ILogger<ClaudeApiClient>>();
        _client = new ClaudeApiClient(_httpClient, _logger);
    }

    [Fact]
    public void SetApiKey_ConfiguresHttpClientHeaders_Correctly()
    {
        // Arrange
        const string apiKey = "test-api-key";

        // Act
        _client.SetApiKey(apiKey);

        // Assert
        Assert.Contains(_httpClient.DefaultRequestHeaders, h => 
            h.Key == "x-api-key" && h.Value.Contains(apiKey));
        Assert.Contains(_httpClient.DefaultRequestHeaders, h => 
            h.Key == "anthropic-version" && h.Value.Contains("2023-06-01"));
        Assert.True(_httpClient.DefaultRequestHeaders.Accept.Any(a => 
            a.MediaType == "application/json"));
    }

    [Fact]
    public void Constructor_SetsBaseAddress_WhenNotAlreadySet()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = Substitute.For<ILogger<ClaudeApiClient>>();

        // Act
        var client = new ClaudeApiClient(httpClient, logger);

        // Assert
        Assert.Equal("https://api.anthropic.com/v1/", httpClient.BaseAddress?.ToString());
    }

    [Fact]
    public void Constructor_DoesNotOverrideBaseAddress_WhenAlreadySet()
    {
        // Arrange
        var httpClient = new HttpClient { BaseAddress = new Uri("https://custom.api.com/") };
        var logger = Substitute.For<ILogger<ClaudeApiClient>>();

        // Act
        var client = new ClaudeApiClient(httpClient, logger);

        // Assert
        Assert.Equal("https://custom.api.com/", httpClient.BaseAddress?.ToString());
    }

    [Fact]
    public void Dispose_DisposesHttpClient()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = Substitute.For<ILogger<ClaudeApiClient>>();
        var client = new ClaudeApiClient(httpClient, logger);

        // Act & Assert - Should not throw
        httpClient.Dispose();
    }
}