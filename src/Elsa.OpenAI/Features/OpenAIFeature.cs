using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.OpenAI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.OpenAI.Features;

/// <summary>
/// Represents a feature for setting up OpenAI integration within the Elsa framework.
/// </summary>
public class OpenAIFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Applies the feature to the specified service collection.
    /// </summary>
    public override void Apply() =>
        Services
            .AddSingleton<OpenAIClientFactory>();
}