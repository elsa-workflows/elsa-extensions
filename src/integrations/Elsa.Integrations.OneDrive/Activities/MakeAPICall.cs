using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Makes an arbitrary API call to Microsoft Graph API.
/// </summary>
[Activity("Elsa", "OneDrive", "Makes an arbitrary API call to Microsoft Graph API.", Kind = ActivityKind.Task)]
public class MakeAPICall : OneDriveActivity<JsonNode>
{
    /// <summary>
    /// The URL path relative to the Microsoft Graph API endpoint (v1.0).
    /// </summary>
    [Input(Description = "The URL path relative to the Microsoft Graph API endpoint (v1.0), e.g. '/me/drive/root/children'.")]
    public Input<string> Path { get; set; } = default!;

    /// <summary>
    /// The HTTP method to use.
    /// </summary>
    [Input(Description = "The HTTP method to use (GET, POST, PUT, DELETE, PATCH).")]
    public Input<string> Method { get; set; } = new("GET");

    /// <summary>
    /// The query parameters to include in the request.
    /// </summary>
    [Input(Description = "The query parameters to include in the request (JSON object).")]
    public Input<string>? QueryParameters { get; set; }

    /// <summary>
    /// The request body for the API call (for POST, PUT, and PATCH requests).
    /// </summary>
    [Input(Description = "The request body for the API call (JSON string for POST, PUT, and PATCH requests).")]
    public Input<string>? Body { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var path = Path.Get(context);
        var method = Method.Get(context)?.ToUpper() ?? "GET";
        var queryParams = QueryParameters?.Get(context);
        var body = Body?.Get(context);

        // Ensure path starts with a forward slash
        if (!path.StartsWith("/"))
        {
            path = $"/{path}";
        }

        // Create the request URL
        var baseUrl = "https://graph.microsoft.com/v1.0";
        var url = $"{baseUrl}{path}";
        
        // Add query parameters if provided
        if (!string.IsNullOrEmpty(queryParams))
        {
            try
            {
                var paramsDict = JsonSerializer.Deserialize<Dictionary<string, string>>(queryParams);
                if (paramsDict?.Count > 0)
                {
                    var queryString = string.Join("&", paramsDict.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                    url = $"{url}?{queryString}";
                }
            }
            catch (JsonException ex)
            {
                throw new ArgumentException($"Invalid query parameters format: {ex.Message}", ex);
            }
        }

        // Create and send the HTTP request
        using var httpClient = graphClient.HttpProvider.GetHttpClient();
        using var httpRequestMessage = new HttpRequestMessage(new HttpMethod(method), url);

        // Add authentication
        await graphClient.AuthenticationProvider.AuthenticateRequestAsync(httpRequestMessage);

        // Add body content for POST, PUT, PATCH
        if (!string.IsNullOrEmpty(body) && (method == "POST" || method == "PUT" || method == "PATCH"))
        {
            httpRequestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        // Send the request
        var response = await httpClient.SendAsync(httpRequestMessage, context.CancellationToken);

        // Process the response
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync(context.CancellationToken);

        // Parse the JSON response
        JsonNode? resultNode = null;
        if (!string.IsNullOrEmpty(responseContent))
        {
            try
            {
                resultNode = JsonNode.Parse(responseContent);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Failed to parse response as JSON: {ex.Message}", ex);
            }
        }

        Result.Set(context, resultNode ?? JsonValue.Create("{}")!);
    }
}