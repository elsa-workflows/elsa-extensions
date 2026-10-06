using Elsa.Workflows;

namespace Elsa.DevOps.AzureDevOps.RuntimeWorkflows;

/// <summary>
/// The metadata Elsa Studio reads to draw a code-defined workflow. Shared by every Azure DevOps system workflow,
/// including the pull request watcher, which has a shape of its own and so does not derive from
/// <see cref="AzureDevOpsPollingWorkflowBase"/>.
/// </summary>
public static class AzureDevOpsWorkflowDesigner
{
    private const double NodeWidth = 220.0;
    private const double NodeHeight = 80.0;

    /// <summary>
    /// Positions an activity on the Elsa Studio design surface.
    /// </summary>
    public static void At(IActivity activity, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(activity);

        activity.Metadata["designer"] = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["position"] = new Dictionary<string, object>(StringComparer.Ordinal) { ["x"] = x, ["y"] = y },
            ["size"] = new Dictionary<string, object>(StringComparer.Ordinal) { ["width"] = NodeWidth, ["height"] = NodeHeight },
        };
    }

    /// <summary>
    /// Gives an activity a stable name and the label shown on the design surface.
    /// </summary>
    public static T Named<T>(T activity, string name, string displayText)
        where T : IActivity
    {
        ArgumentNullException.ThrowIfNull(activity);

        activity.Name = name;
        activity.Metadata["displayText"] = displayText;
        return activity;
    }
}
