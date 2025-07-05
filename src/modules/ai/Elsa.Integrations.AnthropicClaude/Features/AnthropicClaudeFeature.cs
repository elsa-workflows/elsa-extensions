using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.Integrations.AnthropicClaude.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Integrations.AnthropicClaude.Features;

/// <summary>
/// Feature for setting up Anthropic Claude integration within the Elsa framework.
/// </summary>
public class AnthropicClaudeFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Applies the feature to the specified service collection.
    /// </summary>
    public override void Apply()
    {
        Services
            .AddHttpClient("Claude", client =>
            {
                client.BaseAddress = new Uri("https://api.anthropic.com/v1/");
                client.Timeout = TimeSpan.FromMinutes(5); // Claude API calls can take time
            })
            .Services
            .AddSingleton<ClaudeClientFactory>();
    }
}