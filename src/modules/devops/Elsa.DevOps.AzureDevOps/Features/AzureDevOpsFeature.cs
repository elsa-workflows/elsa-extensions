using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.DevOps.AzureDevOps.Services;
using Elsa.DevOps.AzureDevOps.Activities.Builds;
using Elsa.DevOps.AzureDevOps.Activities.PullRequests;
using Elsa.DevOps.AzureDevOps.Activities.Repositories;
using Elsa.DevOps.AzureDevOps.Activities.WorkItems;
using Elsa.DevOps.AzureDevOps.Configuration;
using Elsa.DevOps.AzureDevOps.Triggers;
using Elsa.DevOps.AzureDevOps.Controllers;
using Elsa.Workflows.Management.Features;
using Elsa.Workflows.UIHints.Dictionary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace Elsa.DevOps.AzureDevOps.Features;

/// <summary>
/// Represents a feature for setting up Azure DevOps integration within the Elsa framework.
/// </summary>
public class AzureDevOpsFeature(IModule module) : FeatureBase(module)
{
    private const string VariableTypeCategory = "Azure DevOps";

    /// <summary>
    /// The Azure DevOps WebApi types the activities produce. Registering them as variable types is what lets a
    /// workflow keep an activity result in a variable and address its properties from an expression; without it the
    /// designer has no type to offer and the value can only travel as an untyped object.
    /// </summary>
    private static readonly Type[] VariableTypes =
    [
        typeof(Build),
        typeof(GitBranchStats),
        typeof(GitPullRequest),
        typeof(GitRepository),
        typeof(WorkItem),
        typeof(Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models.Comment),
        // What Work Item Commented hands over alongside the work item. Its own type rather than the WebApi Comment,
        // because a Service Hook payload knows the text and the author but not the comment id - see PostedComment.
        typeof(Events.PostedComment),
        // The list activities hand out collections, which are separate types to the registry.
        typeof(ICollection<Build>),
        typeof(ICollection<GitBranchStats>),
        typeof(ICollection<GitPullRequest>),
        typeof(ICollection<WorkItem>)
    ];

    public Action<AzureDevOpsOptions>? ConfigureOptions { get; set; }

    public Action<AzureDevOpsPollingOptions>? ConfigurePollingOptions { get; set; }

    /// <summary>
    /// Applies the feature to the specified service collection.
    /// </summary>
    public override void Apply()
    {
        base.Apply();
        Module.AddActivitiesFrom<AzureDevOpsFeature>();
        Module.Configure<WorkflowManagementFeature>(management => management.AddVariableTypes(VariableTypes, VariableTypeCategory));
        Services.AddControllers().AddApplicationPart(typeof(AzureDevOpsWebhookController).Assembly);
        Module.AddActivity<BuildCompletedTrigger>();
        Module.AddActivity<BuildInProgressTrigger>();
        Module.AddActivity<BuildQueuedTrigger>();
        Module.AddActivity<CodePushedTrigger>();
        Module.AddActivity<PullRequestCreatedTrigger>();
        Module.AddActivity<PullRequestUpdatedTrigger>();
        Module.AddActivity<PullRequestMergedTrigger>();
        Module.AddActivity<WorkItemCreatedTrigger>();
        Module.AddActivity<WorkItemUpdatedTrigger>();
        Module.AddActivity<WorkItemDeletedTrigger>();
        Module.AddActivity<WorkItemCommentedTrigger>();
        Module.AddActivity<AddWorkItemComment>();
        Module.AddActivity<DisplayWorkItem>();
        Module.AddActivity<AddWorkItemRelation>();
        Module.AddActivity<AddWorkItemHyperlink>();
        Module.AddActivity<PollAzureDevOpsWorkItems>();
        Module.AddWorkflow<RuntimeWorkflows.AzureDevOpsWorkItemPollingWorkflow>();
        Module.AddActivity<DisplayBuild>();
        Module.AddActivity<PollAzureDevOpsBuilds>();
        Module.AddWorkflow<RuntimeWorkflows.AzureDevOpsBuildPollingWorkflow>();
        Module.AddActivity<DisplayPullRequest>();
        Module.AddActivity<PollAzureDevOpsPullRequests>();
        Module.AddWorkflow<RuntimeWorkflows.AzureDevOpsPullRequestPollingWorkflow>();
        Module.AddActivity<InitializePullRequestWatch>();
        Module.AddActivity<WatchAzureDevOpsPullRequest>();
        Module.AddWorkflow<RuntimeWorkflows.AzureDevOpsPullRequestWatcherWorkflow>();
        Module.AddActivity<DisplayRepository>();
        Module.AddActivity<PollAzureDevOpsPushes>();
        Module.AddWorkflow<RuntimeWorkflows.AzureDevOpsPushPollingWorkflow>();
        // UpdateWorkItem points its Fields input at this evaluator to resolve the per-entry expressions the dictionary
        // editor stores. Registering it here keeps the input working regardless of whether the host already has it.
        Services.AddScoped<DictionaryValueEvaluator>();
        Services.AddSingleton<AzureDevOpsConnectionFactory>();
        // Registered here rather than in the package that uses it, so any host with Azure DevOps installed can read a
        // work item and write single fields and comments without going through an activity.
        Services.AddScoped<IWorkItemEditor, VssWorkItemEditor>();
        Services.AddSingleton<AzureDevOpsProjectResolver>();
        Services.AddSingleton<AzureDevOpsOrganizationUrlResolver>();
        // Activities fall back to the PAT of the user the workflow runs for, which is read from the ambient request
        // when the workflow instance carries no caller of its own.
        Services.AddHttpContextAccessor();
        // TryAdd, so a host that reads its credentials from somewhere other than Elsa's own secret store can supply
        // its own reader and keep it. The default here needs ISecretManager, which only exists once the host has
        // enabled the secrets feature - registering it unconditionally would turn "no secrets configured" into a
        // resolution failure on the first activity that looks a token up.
        Services.TryAddScoped<IAzureDevOpsSecretReader, AzureDevOpsSecretReader>();
        Services.AddScoped<AzureDevOpsUserNameResolver>();
        Services.AddScoped<AzureDevOpsTokenResolver>();
        // Answers "which organization, which project, and as whom" for anything that reads or writes a work item
        // outside an activity's own inputs: the agent tool set, and the MCP tools of a host that exposes them.
        Services.AddScoped<WorkItemToolTargetResolver>();
        Services.AddScoped<WorkItemReader>();
        Services.AddScoped<AzureDevOpsWebhookEventHandler>();
        Services.AddScoped<AzureDevOpsWorkItemPollingDispatcher>();
        Services.AddScoped<AzureDevOpsBuildPollingDispatcher>();
        Services.AddScoped<AzureDevOpsPullRequestPollingDispatcher>();
        Services.AddScoped<AzureDevOpsPushPollingDispatcher>();
        Services.AddScoped<AzureDevOpsPollingCredentialResolver>();
        Services.AddScoped<AzureDevOpsPullRequestWatcher>();
        Services.AddScoped<AzureDevOpsPullRequestWatchStarter>();
        Services.AddHostedService<AzureDevOpsPollingWorkflowStarter>();
        Services.Configure(ConfigureOptions ?? (_ => { }));
        Services.Configure(ConfigurePollingOptions ?? (_ => { }));
    }
}
