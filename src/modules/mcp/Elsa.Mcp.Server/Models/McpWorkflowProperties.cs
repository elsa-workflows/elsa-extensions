namespace Elsa.Mcp.Server.Models;

/// <summary>
/// The workflow instance property keys written by the MCP extension.
/// </summary>
public static class McpWorkflowProperties
{
    /// <summary>
    /// Holds the raw <c>Authorization</c> header of the MCP request.
    /// </summary>
    public const string AuthorizationToken = "AuthorizationToken";

    /// <summary>
    /// Holds the name of the authenticated caller that invoked the tool.
    /// </summary>
    public const string UserName = "McpUserName";

    /// <summary>
    /// Holds how many AI agents deep the call chain already is.
    /// </summary>
    /// <remarks>
    /// Read from the <see cref="AgentDepthHeader"/> request header, which an agent calling this server sets to its
    /// own depth plus one. A workflow host that exposes its workflows as MCP tools can be reached by an agent running
    /// inside one of those workflows, so without a counter <c>workflow → agent → tool → workflow → agent</c> has
    /// nothing stopping it. Treated as caller context precisely so that workflow input cannot claim to be at depth
    /// zero and start the chain over.
    /// </remarks>
    public const string AgentDepth = "AgentDepth";

    /// <summary>
    /// The request header the depth travels on.
    /// </summary>
    public const string AgentDepthHeader = "X-Elsa-Agent-Depth";

    /// <summary>
    /// Determines whether a name is one of the caller context properties. Workflow input using such a name is not
    /// mirrored onto the instance properties, so a tool call cannot present itself as another caller.
    /// </summary>
    public static bool IsCallerContextProperty(string name) =>
        string.Equals(name, AuthorizationToken, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, UserName, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, AgentDepth, StringComparison.OrdinalIgnoreCase);
}
