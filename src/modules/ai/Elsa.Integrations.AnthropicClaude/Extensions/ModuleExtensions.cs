using Elsa.Features.Abstractions;
using Elsa.Integrations.AnthropicClaude.Features;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Extensions for configuring Anthropic Claude integration.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Adds Anthropic Claude integration to Elsa.
    /// </summary>
    /// <param name="module">The Elsa module.</param>
    /// <returns>The module for further configuration.</returns>
    public static IModule UseAnthropicClaude(this IModule module)
    {
        module.Configure<AnthropicClaudeFeature>();
        return module;
    }
}