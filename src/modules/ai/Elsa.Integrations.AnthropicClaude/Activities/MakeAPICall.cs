using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Integrations.AnthropicClaude.Activities;

/// <summary>
/// Makes an arbitrary HTTP call to the Anthropic Claude API.
/// </summary>
[Activity(
    "Elsa.AnthropicClaude.API",
    "Anthropic Claude",
    "Performs an arbitrary authorized API call to the Anthropic Claude API.",
    DisplayName = "Make API Call")]
[UsedImplicitly]
public class MakeAPICall : ClaudeActivity
{
    /// <summary>
    /// The HTTP method to use for the API call.
    /// </summary>
    [Input(
        Description = "The HTTP method to use (GET, POST, PUT, DELETE, etc.).",
        DefaultValue = "POST")]
    public Input<string> HttpMethod { get; set; } = new("POST");

    /// <summary>
    /// The API endpoint to call (relative to the Claude API base URL).
    /// </summary>
    [Input(
        Description = "The API endpoint to call, relative to https://api.anthropic.com/v1/ (e.g., 'messages', 'models').",
        DefaultValue = "messages")]
    public Input<string> Endpoint { get; set; } = new("messages");

    /// <summary>
    /// The request body as JSON string (for POST, PUT requests).
    /// </summary>
    [Input(
        Description = "The request body as a JSON string. Leave empty for GET requests.",
        UIHint = "multiline")]
    public Input<string?> RequestBody { get; set; } = null!;

    /// <summary>
    /// Whether to validate that the request body is valid JSON before sending.
    /// </summary>
    [Input(
        Description = "Whether to validate that the request body is valid JSON before sending the request.",
        DefaultValue = true)]
    public Input<bool> ValidateJson { get; set; } = new(true);

    /// <summary>
    /// The raw response body from the API call.
    /// </summary>
    [Output(Description = "The raw response body from the Claude API call.")]
    public Output<string> ResponseBody { get; set; } = null!;

    /// <summary>
    /// Indicates whether the API call was successful (2xx status code).
    /// </summary>
    [Output(Description = "True if the API call was successful (2xx status code), false otherwise.")]
    public Output<bool> Success { get; set; } = null!;

    /// <summary>
    /// Executes the activity.
    /// </summary>
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var httpMethodString = context.Get(HttpMethod)!;
        var endpoint = context.Get(Endpoint)!;
        var requestBody = context.Get(RequestBody);
        var validateJson = context.Get(ValidateJson);

        var client = GetClient(context);

        // Validate HTTP method
        if (!System.Net.Http.HttpMethod.TryParse(httpMethodString, out var method))
        {
            throw new ArgumentException($"Invalid HTTP method: {httpMethodString}");
        }

        // Validate JSON if requested and content is provided
        if (validateJson && !string.IsNullOrWhiteSpace(requestBody))
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(requestBody);
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new ArgumentException($"Invalid JSON in request body: {ex.Message}", ex);
            }
        }

        // Ensure endpoint doesn't start with slash (we'll add it in the service)
        if (endpoint.StartsWith("/"))
        {
            endpoint = endpoint.Substring(1);
        }

        try
        {
            // Make the API call
            var responseBody = await client.MakeApiCallAsync(
                method, 
                endpoint, 
                requestBody, 
                context.CancellationToken);

            // Set outputs
            context.Set(ResponseBody, responseBody);
            context.Set(Success, true);
        }
        catch (HttpRequestException)
        {
            // For HTTP errors, we still want to set Success to false but not throw
            // The error details should be in the exception message
            context.Set(ResponseBody, string.Empty);
            context.Set(Success, false);
            throw; // Re-throw to let the workflow handle the error
        }
    }
}