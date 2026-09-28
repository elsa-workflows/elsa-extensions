using Elsa.Mcp.Abstractions;

namespace Elsa.Mcp.Server.Configuration;

/// <summary>
/// Options for exposing Elsa workflows through the Model Context Protocol.
/// </summary>
public class McpOptions
{
    /// <summary>
    /// The route the MCP endpoints are mapped on. Defaults to <c>/mcp</c>.
    /// </summary>
    public string Route { get; set; } = "/mcp";

    /// <summary>
    /// Whether the MCP endpoints require an authenticated caller. Defaults to <c>true</c>, because every tool
    /// starts or resumes a workflow.
    /// </summary>
    public bool RequireAuthorization { get; set; } = true;

    /// <summary>
    /// The name of the authorization policy applied to the MCP endpoints. When left empty, the application's default
    /// policy is used. Ignored when <see cref="RequireAuthorization"/> is <c>false</c>.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// The authentication scheme that validates the caller's token. Only used when OAuth discovery is enabled,
    /// because the endpoints then challenge through the MCP scheme rather than through the scheme that reads the
    /// token. Defaults to the application's default authenticate scheme.
    /// </summary>
    public string? AuthenticationScheme { get; set; }

    /// <summary>
    /// The authorization servers advertised in the protected resource metadata, as issuer URIs. Adding one turns on
    /// OAuth discovery: an unauthenticated call is then answered with a challenge pointing at
    /// <c>/.well-known/oauth-protected-resource</c>, so a client can find out where to sign in on its own instead of
    /// needing a token pasted into its configuration.
    /// </summary>
    public IList<string> AuthorizationServers { get; set; } = [];

    /// <summary>
    /// The scopes advertised in the protected resource metadata, so a client knows what to ask the authorization
    /// server for.
    /// </summary>
    public IList<string> ScopesSupported { get; set; } = [];

    /// <summary>
    /// The human-readable name of the protected resource, shown by a client while signing in.
    /// </summary>
    public string? ResourceName { get; set; }

    /// <summary>
    /// Whether OAuth discovery is enabled, which it is as soon as an authorization server is advertised.
    /// </summary>
    public bool IsAuthorizationDiscoveryEnabled => AuthorizationServers.Count > 0;

    /// <summary>
    /// The maximum number of workflow tools returned by a single <c>tools/list</c> call.
    /// </summary>
    public int MaxTools { get; set; } = 100;

    /// <summary>
    /// The workflow definition custom property that opts a workflow in as a tool.
    /// </summary>
    public string EnabledCustomPropertyName { get; set; } = McpCustomProperties.Enabled;

    /// <summary>
    /// The workflow definition custom property that names the tool explicitly, overriding the name derived from the
    /// workflow's own name. Ignored when its value holds characters a client would refuse.
    /// </summary>
    public string ToolNameCustomPropertyName { get; set; } = McpCustomProperties.Name;

    /// <summary>
    /// The workflow definition custom property holding instructions for an agent, appended to the tool description.
    /// This is where "use this for a pull request, not for a work item" belongs: guidance a model needs and a person
    /// reading the workflow in the designer does not.
    /// </summary>
    public string InstructionsCustomPropertyName { get; set; } = McpCustomProperties.Instructions;

    /// <summary>
    /// The prefix of the workflow definition custom property holding the instruction for one input, completed with the
    /// input's name — <c>mcp:input:PRId</c>. An input has no custom properties of its own, and its description is what
    /// a person reads in the designer, so an instruction meant for an agent is keyed here instead.
    /// </summary>
    public string InputInstructionCustomPropertyPrefix { get; set; } = McpCustomProperties.InputInstructionPrefix;

    /// <summary>
    /// Exposes every published workflow as a tool, ignoring <see cref="EnabledCustomPropertyName"/>.
    /// </summary>
    public bool ExposeAllPublishedWorkflows { get; set; }

    /// <summary>
    /// Whether the inputs declared on a workflow definition are described in the generated tool input schema.
    /// </summary>
    public bool DescribeWorkflowInputs { get; set; } = true;

    /// <summary>
    /// Whether the caller's name and authorization header are copied onto the workflow instance properties, so that
    /// activities can act on behalf of the caller. See <see cref="Models.McpWorkflowProperties"/> for the keys used.
    /// </summary>
    public bool PropagateCallerContext { get; set; } = true;
}
