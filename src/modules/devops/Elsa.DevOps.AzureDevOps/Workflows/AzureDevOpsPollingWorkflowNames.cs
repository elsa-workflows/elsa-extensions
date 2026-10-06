namespace Elsa.DevOps.AzureDevOps.RuntimeWorkflows;

/// <summary>
/// The definition IDs of the built-in polling workflows. They are literals rather than <c>nameof</c> expressions
/// because a rename of the class must not silently orphan the singleton instances running under the old ID.
/// </summary>
public static class AzureDevOpsPollingWorkflowNames
{
    public const string WorkItems = "AzureDevOpsWorkItemPollingWorkflow";
    public const string Builds = "AzureDevOpsBuildPollingWorkflow";
    public const string PullRequests = "AzureDevOpsPullRequestPollingWorkflow";
    public const string Pushes = "AzureDevOpsPushPollingWorkflow";
    public const string PullRequestWatcher = "AzureDevOpsPullRequestWatcherWorkflow";
}
