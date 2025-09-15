using Elsa.Integrations.AnthropicClaude.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.Integrations.AnthropicClaude.Activities;

/// <summary>
/// Base class for all Claude-related activities.
/// </summary>
public abstract class ClaudeActivity : Activity
{
    /// <summary>
    /// The Anthropic Claude API key.
    /// </summary>
    [Input(
        Description = "The Anthropic Claude API key. Get your API key from https://console.anthropic.com/",
        UIHint = "password")]
    public Input<string> ApiKey { get; set; } = null!;

    /// <summary>
    /// Gets a configured Claude API client.
    /// </summary>
    /// <param name="context">The activity execution context.</param>
    /// <returns>A configured Claude API client.</returns>
    protected ClaudeApiClient GetClient(ActivityExecutionContext context)
    {
        var clientFactory = context.GetRequiredService<ClaudeClientFactory>();
        var apiKey = context.Get(ApiKey)!;
        return clientFactory.GetClient(apiKey);
    }
}