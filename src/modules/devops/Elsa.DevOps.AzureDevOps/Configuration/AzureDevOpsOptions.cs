namespace Elsa.DevOps.AzureDevOps.Configuration;

/// <summary>
/// Organization-wide defaults shared by every Azure DevOps trigger and activity.
/// </summary>
public class AzureDevOpsOptions
{
    /// <summary>
    /// The Azure DevOps organization URL (e.g. https://dev.azure.com/myorg) used by activities and polling that do
    /// not specify one themselves.
    /// </summary>
    public string? DefaultOrganizationUrl { get; set; }

    /// <summary>
    /// The Azure DevOps project, given as either its ID or its name, used by triggers and activities that do not
    /// specify one themselves.
    /// </summary>
    public string? DefaultProject { get; set; }

    /// <summary>
    /// The personal access token used by activities and polling that do not specify one themselves and for which no
    /// per-user token is found. Prefer <see cref="DefaultTokenSecretName"/>, which keeps the token out of
    /// configuration.
    /// </summary>
    public string? DefaultToken { get; set; }

    /// <summary>
    /// The name of the Elsa Secret holding the personal access token used by activities and polling that do not
    /// specify one themselves and for which no per-user token is found. This is the token a workflow that no user
    /// started - one run by the scheduler or by a webhook - falls back to, so it should not hold the token reserved
    /// for a single purpose such as polling.
    /// </summary>
    public string? DefaultTokenSecretName { get; set; }

    /// <summary>
    /// The default placeholder for <see cref="UserTokenSecretNameFormat"/>.
    /// </summary>
    public const string UserPlaceholder = "{user}";

    /// <summary>
    /// The name of the Elsa Secret holding the personal access token of the user an activity runs for, with
    /// <c>{user}</c> standing in for that user's account name. A workflow started by <c>alice@contoso.com</c>
    /// therefore looks for <c>AzureDevOps:alice:Pat</c>, which lets every user act under their own PAT. Applies to
    /// activities only: polling runs for no user and never reads a per-user secret. Leave empty to switch the
    /// per-user lookup off.
    /// </summary>
    public string? UserTokenSecretNameFormat { get; set; } = $"AzureDevOps:{UserPlaceholder}:Pat";

    /// <summary>
    /// Temporary diagnostics for the Service Hook endpoint. See <see cref="AzureDevOpsWebhookDiagnosticsOptions"/>.
    /// </summary>
    public AzureDevOpsWebhookDiagnosticsOptions WebhookDiagnostics { get; set; } = new();

    /// <summary>
    /// How the Service Hook endpoint authenticates a delivery. See <see cref="AzureDevOpsWebhookAuthenticationOptions"/>.
    /// </summary>
    public AzureDevOpsWebhookAuthenticationOptions WebhookAuthentication { get; set; } = new();
}
