using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Elsa.Integrations.AnthropicClaude.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Integrations.AnthropicClaude.Services;

/// <summary>
/// Service for communicating with the Anthropic Claude API.
/// </summary>
public class ClaudeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeApiClient> _logger;
    private const string BaseUrl = "https://api.anthropic.com/v1";

    /// <summary>
    /// Initializes a new instance of the Claude API client.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for API calls.</param>
    /// <param name="logger">The logger instance.</param>
    public ClaudeApiClient(HttpClient httpClient, ILogger<ClaudeApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Set base address if not already set
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }
    }

    /// <summary>
    /// Configures the HTTP client with the provided API key.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    public void SetApiKey(string apiKey)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Creates a completion using the Claude API.
    /// </summary>
    /// <param name="request">The completion request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completion response.</returns>
    public async Task<ClaudeCompletionResponse> CreateCompletionAsync(ClaudeCompletionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            _logger.LogDebug("Sending Claude API request: {Request}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/messages", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Claude API request failed with status {StatusCode}: {Error}", 
                    response.StatusCode, errorContent);
                throw new HttpRequestException($"Claude API request failed: {response.StatusCode} - {errorContent}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received Claude API response: {Response}", responseJson);

            var result = JsonSerializer.Deserialize<ClaudeCompletionResponse>(responseJson, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return result ?? throw new InvalidOperationException("Failed to deserialize Claude API response");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while calling Claude API");
            throw;
        }
    }

    /// <summary>
    /// Makes an arbitrary HTTP request to the Claude API.
    /// </summary>
    /// <param name="method">The HTTP method to use.</param>
    /// <param name="endpoint">The API endpoint (relative to base URL).</param>
    /// <param name="content">The request content (JSON string).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response content as a string.</returns>
    public async Task<string> MakeApiCallAsync(HttpMethod method, string endpoint, string? content = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(method, endpoint);
            
            if (!string.IsNullOrEmpty(content))
            {
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");
            }

            _logger.LogDebug("Making Claude API call: {Method} {Endpoint}", method, endpoint);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API call failed with status {StatusCode}: {Error}", 
                    response.StatusCode, responseContent);
                throw new HttpRequestException($"Claude API call failed: {response.StatusCode} - {responseContent}");
            }

            _logger.LogDebug("Claude API call successful: {Response}", responseContent);
            return responseContent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while making Claude API call");
            throw;
        }
    }
}

/// <summary>
/// Factory for creating Claude API clients with proper configuration.
/// </summary>
public class ClaudeClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ClaudeApiClient> _logger;

    /// <summary>
    /// Initializes a new instance of the Claude client factory.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger instance.</param>
    public ClaudeClientFactory(IHttpClientFactory httpClientFactory, ILogger<ClaudeApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Creates a Claude API client configured with the specified API key.
    /// </summary>
    /// <param name="apiKey">The Anthropic API key.</param>
    /// <returns>A configured Claude API client.</returns>
    public ClaudeApiClient GetClient(string apiKey)
    {
        var httpClient = _httpClientFactory.CreateClient("Claude");
        var client = new ClaudeApiClient(httpClient, _logger);
        client.SetApiKey(apiKey);
        return client;
    }
}