using System.Security.Claims;
using Elsa.Workflows;
using Microsoft.AspNetCore.Http;

namespace Elsa.DevOps.AzureDevOps.Services;

/// <summary>
/// Resolves the account name of the user an activity runs on behalf of, so an activity can authenticate with that
/// user's own personal access token instead of a shared one.
/// </summary>
public class AzureDevOpsUserNameResolver(IHttpContextAccessor httpContextAccessor)
{
    /// <summary>
    /// The workflow instance property the MCP extension writes the caller's name to. Kept as a literal rather than
    /// referencing <c>Elsa.Mcp.Server.Models.McpWorkflowProperties.UserName</c>, so this extension does not take a
    /// dependency on the MCP extension.
    /// </summary>
    private const string McpUserNameProperty = "McpUserName";

    /// <summary>
    /// The claim types that carry a sign-in name, in the order Entra tends to fill them. A display name claim such as
    /// <c>name</c> is deliberately absent: "Alice Anderson" is not an account name, and guessing one from it risks
    /// reading a namesake's token.
    /// </summary>
    private static readonly string[] AccountNameClaimTypes =
    [
        "preferred_username",
        "upn",
        ClaimTypes.Upn,
        "unique_name",
        ClaimTypes.Email,
        "email"
    ];

    /// <summary>
    /// Returns the account name of the user the workflow runs for, stripped of its domain: a caller known as
    /// <c>alice@contoso.com</c> resolves to <c>alice</c>. Returns <c>null</c> for a workflow that no user started,
    /// such as the polling workflow or a webhook-triggered workflow, and for a name that cannot be turned into a
    /// single secret name segment.
    /// </summary>
    public string? Resolve(ActivityExecutionContext context) => ToAccountName(ResolveUserName(context));

    /// <summary>
    /// The same account name for a caller that has no workflow instance behind it: a tool called over MCP, which runs
    /// inside the caller's own request and nowhere else. Returns <c>null</c> when the request is unauthenticated, or
    /// when its claims carry no name that can be turned into a single secret name segment.
    /// </summary>
    public string? ResolveForCaller() => ToAccountName(GetHttpUserName());

    /// <summary>
    /// Prefers the caller of the ambient HTTP request, whose claims carry the sign-in name, over the name recorded on
    /// the workflow instance, which is whichever name the token happened to be identified by. The instance is what is
    /// left to go on for an activity that resumes after a suspension, as that runs on a background thread with no
    /// request to read a caller from.
    /// </summary>
    private string? ResolveUserName(ActivityExecutionContext context) =>
        GetHttpUserName() ?? GetInstanceProperty(context, McpUserNameProperty);

    private static string? GetInstanceProperty(ActivityExecutionContext context, string key) =>
        context.WorkflowExecutionContext.Properties.TryGetValue(key, out object? value) ? Normalize(value?.ToString()) : null;

    private string? GetHttpUserName()
    {
        ClaimsPrincipal? principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        foreach (string claimType in AccountNameClaimTypes)
        {
            string? value = Normalize(principal.FindFirst(claimType)?.Value);

            if (value != null)
                return value;
        }

        return Normalize(principal.Identity.Name);
    }

    /// <summary>
    /// Turns a user name into the segment a secret is named after. Everything from the <c>@</c> onwards is dropped, as
    /// is a <c>DOMAIN\</c> prefix, so the same person resolves to the same name regardless of which claim carried it.
    /// A name that is not a single account name - one holding whitespace, which marks a display name, or a <c>:</c>,
    /// which would split the secret name into another segment - yields <c>null</c> rather than a guess.
    /// </summary>
    private static string? ToAccountName(string? userName)
    {
        if (userName == null)
            return null;

        int domainSeparator = userName.LastIndexOf('\\');

        if (domainSeparator >= 0)
            userName = userName[(domainSeparator + 1)..];

        int at = userName.IndexOf('@');

        if (at >= 0)
            userName = userName[..at];

        userName = userName.Trim();

        if (userName.Length == 0 || userName.Contains(':') || userName.Any(char.IsWhiteSpace))
            return null;

        return userName;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
