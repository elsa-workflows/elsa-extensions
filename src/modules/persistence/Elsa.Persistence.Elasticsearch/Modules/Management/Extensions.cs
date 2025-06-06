using Elsa.Workflows.Management.Features;
using JetBrains.Annotations;

namespace Elsa.Persistence.Elasticsearch.Modules.Management;

/// <summary>
/// Extends the <see cref="WorkflowInstancesFeature"/> feature.
/// </summary>
[PublicAPI]
public static class Extensions
{
    /// <summary>
    /// Configures the <see cref="WorkflowInstancesFeature"/> to use the <see cref="ElasticWorkflowInstanceFeature"/>.
    /// </summary>
    public static WorkflowInstancesFeature UseElasticsearch(this WorkflowInstancesFeature feature, Action<ElasticWorkflowInstanceFeature>? configure = null)
    {
        feature.Module.Configure(configure);
        return feature;
    }
}