using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities;

/// <summary>
/// Generic base class inherited by all Azure DevOps activities.
/// </summary>
public abstract class AzureDevOpsActivity : Workflows.Activity
{
    /// <summary>
    /// The Azure DevOps organization URL (e.g. https://dev.azure.com/myorg).
    /// </summary>
    [Input(Description = "The Azure DevOps organization URL (e.g. https://dev.azure.com/myorg).")]
    public Input<string> OrganizationUrl { get; set; } = null!;

    /// <summary>
    /// The personal access token (PAT) for authentication.
    /// </summary>
    [Input(Description = "The personal access token (PAT) for authentication.")]
    public Input<string> Token { get; set; } = null!;

    /// <summary>
    /// Gets the Azure DevOps connection.
    /// </summary>
    protected VssConnection GetConnection(ActivityExecutionContext context)
    {
        var factory = context.GetRequiredService<AzureDevOpsConnectionFactory>();
        var organizationUrl = context.Get(OrganizationUrl)!;
        var token = context.Get(Token)!;
        return factory.GetConnection(organizationUrl, token);
    }
}
