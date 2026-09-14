using Elsa.DevOps.AzureDevOps.Services;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.VisualStudio.Services.WebApi;

namespace Elsa.DevOps.AzureDevOps.Activities;

/// <summary>
/// Generic base class inherited by all Azure DevOps activities.
/// </summary>
public abstract class AzureDevOpsActivity : Elsa.Workflows.Activity
{
    /// <summary>
    /// Description shared by the <c>Project</c> input of every activity that takes one.
    /// </summary>
    public const string ProjectDescription = "The project name or ID. Falls back to the configured default project (AzureDevOps:DefaultProject) when left empty.";

    /// <summary>
    /// Description shared by the <c>OrganizationUrl</c> input of every activity.
    /// </summary>
    public const string OrganizationUrlDescription = "The Azure DevOps organization URL (e.g. https://dev.azure.com/myorg). Falls back to the configured default organization URL (AzureDevOps:DefaultOrganizationUrl) when left empty.";

    /// <summary>
    /// The Azure DevOps organization URL (e.g. https://dev.azure.com/myorg). Falls back to the configured default
    /// organization URL when left empty.
    /// </summary>
    [Input(Description = OrganizationUrlDescription)]
    public Input<string> OrganizationUrl { get; set; } = null!;

    /// <summary>
    /// Description shared by the <c>Token</c> input of every activity.
    /// </summary>
    public const string TokenDescription = "The personal access token (PAT) for authentication. Falls back to the secret holding the PAT of the user the workflow runs for (AzureDevOps:<user>:Pat), and then to the configured default token (AzureDevOps:DefaultToken or the secret named by AzureDevOps:DefaultTokenSecretName), when left empty.";

    /// <summary>
    /// The personal access token (PAT) for authentication. Falls back to the configured default token when left empty.
    /// </summary>
    [Input(Description = TokenDescription)]
    public Input<string> Token { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context)
    {
        var organizationUrl = ResolveOrganizationUrl(context);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateUri(organizationUrl, nameof(OrganizationUrl)));
        var token = await ResolveTokenAsync(context);
        ActivityInputValidation.ThrowIfInvalid(context, ActivityInputValidation.TryValidateRequired(token, nameof(Token)));
        return await base.CanExecuteAsync(context);
    }

    /// <summary>
    /// Resolves the project to work against, falling back to the configured default project when the activity does
    /// not specify one.
    /// </summary>
    protected static string? ResolveProject(ActivityExecutionContext context, string? project) =>
        context.GetRequiredService<AzureDevOpsProjectResolver>().Resolve(project);

    /// <summary>
    /// Resolves the organization URL to connect to, falling back to the configured default organization URL when the
    /// activity does not specify one.
    /// </summary>
    protected string? ResolveOrganizationUrl(ActivityExecutionContext context) =>
        context.GetRequiredService<AzureDevOpsOrganizationUrlResolver>().Resolve(context.Get(OrganizationUrl));

    /// <summary>
    /// Resolves the token to authenticate with, falling back to the secret holding the personal access token of the
    /// user the workflow runs for, and then to the configured default token, when the activity does not specify one.
    /// </summary>
    protected ValueTask<string?> ResolveTokenAsync(ActivityExecutionContext context) =>
        context.GetRequiredService<AzureDevOpsTokenResolver>().ResolveForActivityAsync(context, context.Get(Token));

    /// <summary>
    /// Gets the Azure DevOps connection.
    /// </summary>
    protected async ValueTask<VssConnection> GetConnectionAsync(ActivityExecutionContext context)
    {
        var organizationUrl = ResolveOrganizationUrl(context);
        var token = await ResolveTokenAsync(context);
        ActivityInputValidation.ThrowIfInvalidUri(organizationUrl, nameof(OrganizationUrl));
        ActivityInputValidation.ThrowIfNullOrEmpty(token, nameof(Token));
        var factory = context.GetRequiredService<AzureDevOpsConnectionFactory>();
        return factory.GetConnection(organizationUrl!, token!);
    }
}
