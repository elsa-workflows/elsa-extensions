using Elsa.Mcp.Server.Services;
using Elsa.Workflows.Management.Entities;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Elsa.Mcp.Server.Handlers;

/// <summary>
/// The MCP request handlers that expose workflows as tools.
/// </summary>
internal static class WorkflowToolRequestHandlers
{
    public static async ValueTask<ListToolsResult> ListToolsAsync(RequestContext<ListToolsRequestParams> requestContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        WorkflowToolCatalog catalog = GetRequiredService<WorkflowToolCatalog>(requestContext.Services);
        IReadOnlyList<Tool> tools = await catalog.ListToolsAsync(cancellationToken).ConfigureAwait(false);

        return new ListToolsResult
        {
            Tools = [.. tools]
        };
    }

    public static async ValueTask<CallToolResult> CallToolAsync(RequestContext<CallToolRequestParams> requestContext, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestContext);

        string? toolName = requestContext.Params?.Name;

        if (string.IsNullOrWhiteSpace(toolName))
            return WorkflowToolInvoker.CreateErrorResult("Unknown tool.");

        WorkflowToolCatalog catalog = GetRequiredService<WorkflowToolCatalog>(requestContext.Services);
        WorkflowDefinition? definition = await catalog.FindExposedDefinitionAsync(toolName, cancellationToken).ConfigureAwait(false);

        // A tool name that is not an exposed workflow is reported as unavailable rather than as a workflow failure:
        // unpublishing or opting a workflow out takes effect between listing and calling.
        if (definition == null)
            return WorkflowToolInvoker.CreateErrorResult($"Tool '{toolName}' is not available.");

        WorkflowToolInvoker invoker = GetRequiredService<WorkflowToolInvoker>(requestContext.Services);

        // The resolved definition is what the invoker runs. The tool name it was found by is a workflow name reduced to
        // what a client accepts, so passing the name on would look up a definition that does not exist.
        return await invoker.InvokeAsync(requestContext.Params!, definition.DefinitionId, requestContext.User, cancellationToken).ConfigureAwait(false);
    }

    private static TService GetRequiredService<TService>(IServiceProvider? services)
        where TService : notnull
    {
        if (services == null)
            throw new InvalidOperationException("MCP request services are not available.");

        return services.GetRequiredService<TService>();
    }
}
