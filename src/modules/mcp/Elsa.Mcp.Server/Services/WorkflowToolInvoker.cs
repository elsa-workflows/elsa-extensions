using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Elsa.Bookmarks.Ui.Models;
using Elsa.Bookmarks.Ui.Services;
using Elsa.Common.Models;
using Elsa.Mcp.Server.Configuration;
using Elsa.Mcp.Server.Models;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Elsa.Workflows.Runtime.Responses;
using Elsa.Workflows.State;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;

namespace Elsa.Mcp.Server.Services;

/// <summary>
/// Starts and resumes workflows on behalf of an MCP tool call.
/// </summary>
public class WorkflowToolInvoker(
    IWorkflowRuntime workflowRuntime,
    IWorkflowDispatcher workflowDispatcher,
    IWorkflowInstanceStore instanceStore,
    IHttpContextAccessor httpContextAccessor,
    IOptions<McpOptions> options,
    ILogger<WorkflowToolInvoker> logger,
    BookmarkDescriptionFactory bookmarkDescriptionFactory,
    IBookmarkUiMapper bookmarkUiMapper)
{
    /// <summary>
    /// Starts the workflow behind the tool, or resumes a suspended instance when the call carries both a workflow
    /// instance id and a bookmark id.
    /// </summary>
    /// <param name="toolRequest">The tool call, whose name is what the caller knows the tool by and is used in what it is told.</param>
    /// <param name="definitionId">The workflow to run, as resolved from the tool name by <see cref="WorkflowToolCatalog"/>. The tool name is no longer the definition id, so it cannot stand in for one.</param>
    /// <param name="user">The caller, whose identity is carried onto the workflow instance.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async ValueTask<CallToolResult> InvokeAsync(CallToolRequestParams toolRequest, string definitionId, ClaimsPrincipal? user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(toolRequest);

        try
        {
            return McpToolArguments.IsResumeRequest(toolRequest.Arguments)
                ? await ResumeAsync(toolRequest, definitionId, user, cancellationToken).ConfigureAwait(false)
                : await StartAsync(toolRequest, definitionId, user, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to invoke workflow tool {ToolName}.", toolRequest.Name);

            // The exception is logged, not returned: its message can carry connection strings, identifiers and other
            // details that the tool caller has no business seeing.
            return CreateErrorResult($"Tool '{toolRequest.Name}' failed. See the server logs for details.");
        }
    }

    /// <summary>
    /// Builds an error result for a tool call that never reached a workflow.
    /// </summary>
    public static CallToolResult CreateErrorResult(string message, string? status = null)
    {
        Dictionary<string, object?> payload = new(StringComparer.Ordinal)
        {
            ["message"] = message
        };

        if (!string.IsNullOrWhiteSpace(status))
            payload["status"] = status;

        return CreateResult(payload, isError: true);
    }

    private async ValueTask<CallToolResult> StartAsync(CallToolRequestParams toolRequest, string definitionId, ClaimsPrincipal? user, CancellationToken cancellationToken)
    {
        Dictionary<string, object> input = McpToolArguments.BuildInput(toolRequest.Arguments);
        Dictionary<string, object> properties = new(StringComparer.OrdinalIgnoreCase);

        // Input is mirrored onto the instance properties, so activities that read from either place see the same call.
        // The caller context is applied afterwards and its keys are skipped here, so an argument named after one of
        // them cannot pass itself off as the caller's identity or token.
        foreach ((string key, object value) in input)
        {
            if (!McpWorkflowProperties.IsCallerContextProperty(key))
                properties[key] = value;
        }

        ApplyCallerContext(properties, user);

        CreateWorkflowInstanceRequest createRequest = new()
        {
            WorkflowDefinitionHandle = new WorkflowDefinitionHandle
            {
                DefinitionId = definitionId,
                VersionOptions = ToVersionOptions(McpToolArguments.GetString(toolRequest.Arguments, "versionOptions"))
            },
            CorrelationId = McpToolArguments.GetString(toolRequest.Arguments, "correlationId"),
            Input = input,
            Properties = properties
        };

        IWorkflowClient client = await workflowRuntime.CreateClientAsync(cancellationToken).ConfigureAwait(false);
        await client.CreateInstanceAsync(createRequest, cancellationToken).ConfigureAwait(false);

        if (McpToolArguments.GetBoolean(toolRequest.Arguments, "dispatchWorkflow"))
        {
            DispatchWorkflowInstanceRequest dispatchRequest = new(client.WorkflowInstanceId);
            DispatchWorkflowResponse dispatchResponse = await workflowDispatcher.DispatchAsync(dispatchRequest, null, cancellationToken).ConfigureAwait(false);

            return dispatchResponse.Succeeded
                ? CreateResult(
                    new Dictionary<string, object?>(StringComparer.Ordinal)
                    {
                        ["definitionId"] = definitionId,
                        ["workflowInstanceId"] = client.WorkflowInstanceId,
                        ["status"] = WorkflowStatus.Running.ToString(),
                        ["subStatus"] = WorkflowSubStatus.Pending.ToString()
                    },
                    isError: false)
                : CreateErrorResult($"Tool '{toolRequest.Name}' could not be dispatched.");
        }

        RunWorkflowInstanceRequest runRequest = new()
        {
            Input = createRequest.Input,
            Properties = createRequest.Properties
        };

        RunWorkflowInstanceResponse response = await client.RunInstanceAsync(runRequest, cancellationToken).ConfigureAwait(false);

        return await ToCallToolResultAsync(definitionId, client, response, null, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<CallToolResult> ResumeAsync(CallToolRequestParams toolRequest, string definitionId, ClaimsPrincipal? user, CancellationToken cancellationToken)
    {
        string workflowInstanceId = McpToolArguments.GetString(toolRequest.Arguments, "workflowInstanceId")!;
        string bookmarkId = McpToolArguments.GetBookmarkId(toolRequest.Arguments)!;
        string? answersJson = McpToolArguments.GetAnswersJson(toolRequest.Arguments);

        // A resume is routed by instance id and bookmark id, both of which are opaque to the tool. Without this check
        // a caller that knows an id could resume an instance of any other workflow through any tool it may call.
        IEnumerable<WorkflowInstanceSummary> instances = await instanceStore
            .SummarizeManyAsync(new WorkflowInstanceFilter { Id = workflowInstanceId }, cancellationToken)
            .ConfigureAwait(false);
        WorkflowInstanceSummary? instance = instances.FirstOrDefault();

        if (instance == null)
            return CreateErrorResult($"Workflow instance '{workflowInstanceId}' was not found.");

        if (!string.Equals(instance.DefinitionId, definitionId, StringComparison.Ordinal))
            return CreateErrorResult($"Workflow instance '{workflowInstanceId}' does not belong to tool '{toolRequest.Name}'.");

        IWorkflowClient client = await workflowRuntime.CreateClientAsync(workflowInstanceId, cancellationToken).ConfigureAwait(false);
        WorkflowState? currentState = await TryExportStateAsync(client, cancellationToken).ConfigureAwait(false);
        Bookmark? bookmark = currentState?.Bookmarks.FirstOrDefault(candidate => string.Equals(candidate.Id, bookmarkId, StringComparison.Ordinal));

        IReadOnlyList<string> notes = [];

        if (bookmark != null)
        {
            BookmarkUiContext uiContext = BookmarkActivityStateResolver.CreateContext(currentState!, instance.DefinitionId, bookmark);
            BookmarkUiDescription? description = await bookmarkUiMapper.DescribeWithProviderAsync(uiContext, cancellationToken).ConfigureAwait(false);

            // No description at all is the mapper saying this bookmark is not a task: a trigger, waiting for another
            // system to raise its event. Refused for the same reason as a missing schema below, and named rather than
            // described, because there is no view to quote.
            if (description == null)
                return CreateErrorResult($"This task cannot be answered: '{bookmark.Name}' is waiting for an external event.");

            // A bookmark whose provider offers no schema is one nobody should be answering - a delay, most of the
            // time. Refusing here is the difference between a workflow that waits and one that ran early.
            if (description.View.Resume == null)
                return CreateErrorResult($"This task cannot be answered: {description.View.Text}");

            BookmarkResumeValidationResult validation = BookmarkResumeValidator.Validate(description.View.Resume, answersJson);

            if (!validation.IsValid)
                return CreateErrorResult(string.Join(" ", validation.Messages));

            // An accepted answer can still carry messages - a field that was dropped because the schema never asked
            // for it. Reporting those only on the failure path is how a caller's silently discarded answer stays
            // silent, which is the mistake "drop it with a sentence" was chosen to avoid in the first place.
            notes = validation.Messages;

            // Written from the validated values, so what reaches the workflow is what the schema described rather
            // than whatever shape the caller sent. How those values travel is the waiting activity's business, so the
            // provider that produced this view writes them when it knows better; the flat field map is the default,
            // and is what UIInteraction's own reader normalises.
            answersJson = description.Provider is IBookmarkResumeWriter writer
                ? writer.WriteAnswers(uiContext, validation.Values)
                : JsonSerializer.Serialize(validation.Values, WorkflowRunResultMapper.SerializerOptions);
        }

        Dictionary<string, object> input = new(StringComparer.Ordinal);

        // RunTask - the activity behind a suspended user task - reads its result from this well-known input key.
        if (!string.IsNullOrEmpty(answersJson))
            input[RunTask.InputKey] = answersJson;

        Dictionary<string, object> properties = new(StringComparer.OrdinalIgnoreCase);
        ApplyCallerContext(properties, user);

        RunWorkflowInstanceRequest runRequest = new()
        {
            BookmarkId = bookmarkId,
            Input = input,
            Properties = properties
        };

        RunWorkflowInstanceResponse response = await client.RunInstanceAsync(runRequest, cancellationToken).ConfigureAwait(false);

        return await ToCallToolResultAsync(definitionId, client, response, notes, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the caller's identity onto the instance properties. Called last, so it wins over anything the caller
    /// passed as input.
    /// </summary>
    private void ApplyCallerContext(Dictionary<string, object> properties, ClaimsPrincipal? user)
    {
        if (!options.Value.PropagateCallerContext)
            return;

        string authorizationToken = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(authorizationToken))
            properties[McpWorkflowProperties.AuthorizationToken] = authorizationToken;

        string? userName = user?.Identity?.Name ?? user?.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(userName))
            properties[McpWorkflowProperties.UserName] = userName;

        // Only a well-formed, non-negative number is carried over. An absent or unreadable header leaves the property
        // unset, which every reader treats as depth zero - the right answer for the overwhelmingly common case of a
        // tool call that no agent made.
        string? depthHeader = httpContextAccessor.HttpContext?.Request.Headers[McpWorkflowProperties.AgentDepthHeader].ToString();

        if (int.TryParse(depthHeader, NumberStyles.Integer, CultureInfo.InvariantCulture, out int depth) && depth >= 0)
            properties[McpWorkflowProperties.AgentDepth] = depth;
    }

    private async ValueTask<CallToolResult> ToCallToolResultAsync(
        string definitionId,
        IWorkflowClient client,
        RunWorkflowInstanceResponse response,
        IReadOnlyList<string>? notes,
        CancellationToken cancellationToken)
    {
        WorkflowState? state = await TryExportStateAsync(client, cancellationToken).ConfigureAwait(false);
        bool isError = WorkflowRunResultMapper.IsError(response, state);

        // A finished workflow has no open bookmarks worth describing, so the providers are not asked about burnt ones.
        IReadOnlyList<WorkflowBookmarkDescription>? described = response.Status == WorkflowStatus.Finished
            ? null
            : await bookmarkDescriptionFactory
                .DescribeAsync(state, state is { Bookmarks.Count: > 0 } ? state.Bookmarks : response.Bookmarks, definitionId, cancellationToken)
                .ConfigureAwait(false);

        try
        {
            return CreateResult(WithNotes(WorkflowRunResultMapper.BuildPayload(definitionId, response, state, described), notes), isError);
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            // Output and bookmark payloads are whatever the workflow put there, so a value that will not serialize must
            // cost the caller the state, not the whole result.
            logger.LogWarning(exception, "Failed to serialize the state of workflow instance {WorkflowInstanceId}.", response.WorkflowInstanceId);
            return CreateResult(WithNotes(WorkflowRunResultMapper.BuildPayload(definitionId, response, null), notes), isError);
        }
    }

    /// <summary>
    /// Adds the validator's non-fatal sentences to a successful result. Absent when there are none, so a caller that
    /// answered exactly what was asked sees nothing extra.
    /// </summary>
    private static Dictionary<string, object?> WithNotes(Dictionary<string, object?> payload, IReadOnlyList<string>? notes)
    {
        if (notes is { Count: > 0 })
            payload["notes"] = notes;

        return payload;
    }

    /// <summary>
    /// Reads the state the run left behind. Failing to read it is not worth failing the tool call over: the caller
    /// still learns how the run ended, only without output, bookmarks and incidents.
    /// </summary>
    private async ValueTask<WorkflowState?> TryExportStateAsync(IWorkflowClient client, CancellationToken cancellationToken)
    {
        try
        {
            return await client.ExportStateAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to read the state of workflow instance {WorkflowInstanceId}.", client.WorkflowInstanceId);
            return null;
        }
    }

    private static CallToolResult CreateResult(Dictionary<string, object?> payload, bool isError)
    {
        JsonElement structuredContent = JsonSerializer.SerializeToElement(payload, WorkflowRunResultMapper.SerializerOptions);

        return new CallToolResult
        {
            // The same payload is sent as text as well, for clients that do not read structured content.
            Content = [new TextContentBlock { Text = structuredContent.GetRawText() }],
            IsError = isError,
            StructuredContent = structuredContent
        };
    }

    private static VersionOptions ToVersionOptions(string? versionOptions) =>
        string.IsNullOrWhiteSpace(versionOptions) ? VersionOptions.Published : VersionOptions.FromString(versionOptions);
}
