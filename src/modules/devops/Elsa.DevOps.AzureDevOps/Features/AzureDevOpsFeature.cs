using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.DevOps.AzureDevOps.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.DevOps.AzureDevOps.Features;

/// <summary>
/// Represents a feature for setting up Azure DevOps integration within the Elsa framework.
/// </summary>
public class AzureDevOpsFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Applies the feature to the specified service collection.
    /// </summary>
    public override void Apply()
    {
        Module.AddActivitiesFrom<AzureDevOpsFeature>();
        Services.AddSingleton<AzureDevOpsConnectionFactory>();
    }
}
