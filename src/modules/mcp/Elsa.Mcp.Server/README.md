# Elsa.Mcp.Server Extension

<details>
  <summary>Table of Contents</summary>
  <ol>
    <li><a href="#overview">Overview</a></li>
    <li><a href="#features">Features</a></li>
    <li>
      <a href="#getting-started">Getting Started</a>
      <ul>
        <li><a href="#prerequisites">Prerequisites</a></li>
        <li><a href="#installation">Installation</a></li>
        <li><a href="#configuration">Configuration</a></li>
      </ul>
    </li>
    <li><a href="#authentication">Authentication</a></li>
    <li><a href="#tools">Tools</a></li>
    <li><a href="#examples">Examples</a></li>
    <li><a href="#limitations">Limitations</a></li>
    <li><a href="#troubleshooting">Troubleshooting</a></li>
    <li><a href="#planned-features">Planned Features</a></li>
    <li><a href="#references">References</a></li>
  </ol>
</details>

## Overview

This package extends [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) with a **Model Context Protocol (MCP)** server. Published workflows that are opted in become MCP tools, so an MCP client - an agent, an IDE, or a chat application - can list them, start them, and resume them over HTTP.

## Features

- MCP server over HTTP, mapped on a configurable route (`/mcp` by default)
- Every published workflow with the `mcp:enabled` custom property is exposed as a tool, discovered per request
- Tools named after the workflow rather than after its definition id, so a model can tell one from another
- Instructions for an agent, on the tool and on each of its inputs, written as custom properties on the definition
- Tool input schemas generated from the inputs declared on the workflow definition
- Starting a workflow, in the foreground (awaiting the first suspension point) or dispatched to the background
- Resuming a suspended workflow instance through its bookmark, with answers for the suspended activity
- Built-in tools to find instances and the bookmarks to resume them with: a full search, plus fixed-filter shortcuts for pending, running and completed work
- Open bookmarks described by their own feature through `Elsa.Bookmarks.Ui`: what to show, what to ask, and whether the bookmark is answerable at all
- Authorization on the MCP endpoints, and the caller's identity carried onto the workflow instance

## Getting Started

### Prerequisites

- Elsa Workflows (e.g. V3) installed in your project
- ASP.NET Core routing and, unless authorization is turned off, authentication configured in the host

### Installation

Add the Elsa MCP extension to your project:

```bash
dotnet add package Elsa.Mcp.Server
```

### Configuration

Register the MCP extension in your Elsa builder (e.g. in `Program.cs` or `Startup.cs`), and map its endpoints next to your other endpoints:

```csharp
services
    .AddElsa(elsa =>
    {
        elsa.UseMcpServer();
        // Other Elsa configurations...
    });

// ...

app.MapMcpServer();
```

Options can be set from code, from configuration, or from both:

```csharp
elsa.UseMcpServer(feature =>
{
    feature.IncludeWorkflowInstanceTools = true;
    feature.ConfigureTools = builder => builder.WithTools<MyTools>();
    feature.ConfigureOptions = options =>
    {
        options.Route = "/mcp";
        options.AuthorizationPolicy = "McpPolicy";
        options.MaxTools = 100;
    };
});

services.Configure<McpOptions>(configuration.GetSection("Mcp"));
```

| Option | Default | Description |
|--------|---------|-------------|
| `Route` | `/mcp` | The route the MCP endpoints are mapped on. |
| `RequireAuthorization` | `true` | Whether the endpoints require an authenticated caller. |
| `AuthorizationPolicy` | *(empty)* | The policy applied to the endpoints. Empty means the application's default policy. |
| `AuthorizationServers` | *(empty)* | Issuer URIs advertised in the protected resource metadata. Adding one turns on OAuth discovery. |
| `ScopesSupported` | *(empty)* | The scopes advertised in the metadata, so a client knows what to ask for. |
| `AuthenticationScheme` | *(default scheme)* | The scheme that validates the caller's token when discovery is on. |
| `ResourceName` | *(empty)* | The human-readable resource name a client shows while signing in. |
| `MaxTools` | `100` | The maximum number of workflow tools returned by one `tools/list` call. |
| `EnabledCustomPropertyName` | `mcp:enabled` | The workflow definition custom property that opts a workflow in. |
| `ToolNameCustomPropertyName` | `mcp:name` | The custom property that names the tool explicitly, instead of deriving the name from the workflow's name. |
| `InstructionsCustomPropertyName` | `mcp:instructions` | The custom property holding instructions for an agent, appended to the tool description. |
| `InputInstructionCustomPropertyPrefix` | `mcp:input:` | The prefix of the custom property holding one input's instruction, completed with the input's name. |
| `ExposeAllPublishedWorkflows` | `false` | Exposes every published workflow, ignoring the opt-in property. |
| `DescribeWorkflowInputs` | `true` | Describes the declared workflow inputs in the tool input schema. |
| `PropagateCallerContext` | `true` | Copies the caller's name and authorization header onto the workflow instance. |

`IncludeWorkflowInstanceTools` and `ConfigureTools` are feature properties rather than options, because registering a tool type happens while the service collection is built.

### Tools of the host

`ConfigureTools` hands the host the same `IMcpServerBuilder` the built-in tools are registered on, so a host can offer tool types of its own next to the workflow tools:

```csharp
elsa.UseMcpServer(feature => feature.ConfigureTools = builder => builder.WithTools<WorkItemMcpTools>());
```

Use it rather than calling `AddMcpServer()` a second time in the host: this feature owns the HTTP transport and both request handlers, and a second call registers all three again. A tool type registered here is fixed for the lifetime of the process — unlike the workflow tools, which are resolved per request — and appears in the same `tools/list` answer.

### Opting a workflow in

A workflow becomes a tool when its definition carries the custom property `mcp:enabled`:

```json
{
  "customProperties": {
    "mcp:enabled": true
  }
}
```

`true`, `"true"` and `"1"` all count as enabled. Only **published** versions are exposed, and the list is resolved on every `tools/list` and `tools/call`, so publishing, unpublishing and opting out take effect immediately - no restart, no cache to clear.

> **A designer, not just JSON.** `Elsa.Studio.Mcp` adds a section to the workflow designer's Properties tab, tab
> **Properties**, where an author fills in the four custom properties below without typing JSON by hand. The names
> and the naming rule this section shows both live in `Elsa.Mcp.Abstractions`, the package this server delegates to
> for the same names and rule — so the editor and this server always agree on what a value means.

### Naming a tool

The tool name is derived from the workflow's **name**: accents folded, everything a client will not accept turned into a dash. `PR - Review taak` becomes `pr-review-taak`.

This is what a model has to go on when it picks a tool, so it is worth the workflow being named after what it does. A workflow drawn in the designer gets a generated definition id such as `49ba274a987b98d4`, and naming a tool after that put two unrelated tools side by side as indistinguishable hex strings - which is how a request to start a PR review ends up starting something else.

Override it when the derived name is not the name you want a model to call:

```json
{
  "customProperties": {
    "mcp:enabled": true,
    "mcp:name": "start_pr_review"
  }
}
```

An explicit name is taken as written, as long as a client would accept it - letters, digits, underscores and dashes, at most 64 characters. One that would not is ignored in favour of the derived name, so a typo does not take the tool out of the list.

Two things follow from a name that is derived from an editable field:

- **The definition id keeps working as a tool name.** `tools/call` resolves the id first and the derived name second, so a client that learned the previous hex name is not broken by this, and an id remains the one name that never moves.
- **Two workflows may share a name.** Letting both answer to it would send a call to whichever the store returned first, so every workflow in the clash is named `{name}-{definitionId}` instead. Renaming one of them, or giving it an `mcp:name`, gives both back their short names.

### Telling an agent how to use a tool

The tool description is the workflow's description, falling back to its name. Beyond that, `mcp:instructions` is appended to it - this is where guidance belongs that a model needs and a person reading the workflow in the designer does not:

```json
{
  "customProperties": {
    "mcp:enabled": true,
    "mcp:instructions": "Use this for a pull request, not for a work item. PRId is a pull request id, not a work item id.",
    "mcp:input:PRId": "The id of the pull request in Azure DevOps."
  }
}
```

`mcp:input:<name>` does the same for one input. An input has no custom properties of its own - Elsa's `ArgumentDefinition` carries only a name, display name, description, category and type - so the instruction is keyed by input name on the definition. It wins over the input's description, which stays the text a person reads in the designer.

Without it, an input is described by its **description**, falling back to its **display name**. Both reach the model, which is easy to miss: a display name like `Pullrequest Id` is all a model is told about the argument unless one of these is filled in.

## Authentication

The MCP endpoints require an authenticated caller by default. Configure authentication in the host as usual and, when the MCP endpoints need their own policy, name it through `AuthorizationPolicy`:

```csharp
services.AddAuthorization(options =>
    options.AddPolicy("McpPolicy", policy => policy.RequireAuthenticatedUser()));

elsa.UseMcpServer(feature => feature.ConfigureOptions = options => options.AuthorizationPolicy = "McpPolicy");
```

### Letting a client sign in on its own

By default an unauthenticated call is answered with a bare `WWW-Authenticate: Bearer`, which tells a client that it needs a token but not where to get one - so the token has to be configured by hand. Advertising an authorization server turns the endpoints into a discoverable OAuth protected resource instead:

```csharp
elsa.UseMcpServer(feature => feature.ConfigureOptions = options =>
{
    options.AuthorizationServers.Add("https://login.microsoftonline.com/<tenant>/v2.0");
    options.ScopesSupported.Add("api://<api>/<scope>");
    options.AuthenticationScheme = "Bearer";
});
```

Two things then change. The challenge points at the metadata: `WWW-Authenticate: Bearer resource_metadata="https://<host>/.well-known/oauth-protected-resource/mcp"`. And that document, served anonymously next to the endpoints, names the authorization server and the scopes:

```json
{
  "resource": "https://<host>/mcp",
  "authorization_servers": ["https://login.microsoftonline.com/<tenant>/v2.0"],
  "bearer_methods_supported": ["header"],
  "scopes_supported": ["api://<api>/<scope>"]
}
```

A client that implements the MCP authorization spec - VS Code, for instance - reads this and runs the sign-in flow itself.

The token is still validated by the host's own scheme: the MCP scheme only owns the challenge and forwards authentication to `AuthenticationScheme`, which defaults to the application's default authenticate scheme. `resource` is filled in from the request, so a server reachable under more than one host name identifies itself correctly under each.

Note that `AuthorizationPolicy` keeps its requirements when discovery is on; only the scheme that answers an unauthenticated call changes.

With `PropagateCallerContext` enabled, every workflow started or resumed through MCP receives these instance properties, so activities can act on behalf of the caller:

| Property | Content |
|----------|---------|
| `AuthorizationToken` | The raw `Authorization` header of the MCP request |
| `McpUserName` | The caller's name, or its `sub` claim |
| `AgentDepth` | How many AI agents deep the call chain is, from the `X-Elsa-Agent-Depth` request header |

`AgentDepth` exists because a host that exposes its workflows here can also *run* an AI agent inside one of those workflows — see `Elsa.Ai.Agent`. That makes `workflow → agent → tool → workflow → agent` reachable, and the counter is what bounds it: each agent sends its own depth plus one, and refuses to run once the configured maximum is reached. An absent or unreadable header leaves the property unset, which every reader treats as depth zero — the right answer for the overwhelmingly common case of a tool call that no agent made.

All three are caller context, so an input argument of the same name is not mirrored onto the instance properties. For `AgentDepth` that matters as much as it does for the token: a call that could declare itself to be at depth zero would restart the recursion budget on every hop.

## Tools

### Workflow tools

Each exposed workflow is one tool, named after the workflow — see [Naming a tool](#naming-a-tool) — with its title and description taken from the definition. A call starts the workflow, unless it carries both `workflowInstanceId` and `bookmarkId`, in which case it resumes that instance.

Arguments that are not in the table below are passed to the workflow as input, so a tool can be called with the inputs the workflow declares. The following argument names are reserved:

| Argument | Type | Description |
|----------|------|-------------|
| `additionalData` | object | Extra workflow input, merged with the arguments passed at the top level |
| `answers` | object | Answers handed to the resumed activity |
| `answersJson` | string | Answers handed to the resumed activity, as a JSON string |
| `bookmarkId` | string | The bookmark to resume; requires `workflowInstanceId` |
| `correlationId` | string | Correlation id for the new workflow instance |
| `dispatchWorkflow` | boolean | Runs the workflow in the background instead of awaiting its first suspension point |
| `versionOptions` | string | The workflow version to start; defaults to the published version |
| `workflowInstanceId` | string | The workflow instance to resume; requires `bookmarkId` |

When neither `answers` nor `answersJson` is given on a resume call, the remaining arguments are collected into the answers, so a caller can answer a task by simply passing the answered fields.

Every call returns the same structured result (also repeated as text, for clients that do not read structured content):

```json
{
  "definitionId": "order-intake",
  "workflowInstanceId": "8f2e...",
  "status": "Running",
  "subStatus": "Suspended",
  "output": { "orderId": 4711 },
  "bookmarkId": "b41c...",
  "bookmarkIds": ["b41c..."],
  "output": { "orderId": 4711 },
  "incidents": []
}
```

`bookmarkId` is the bookmark to resume the instance with. `output` holds the workflow's declared outputs and is present only when the run produced any — it is how a caller learns what the workflow actually did, rather than only that it finished. `incidents` is present only when the run produced them, and marks the result as an error.

### Described bookmarks

Every open bookmark in `bookmarks[]` is passed through the providers registered by `Elsa.Bookmarks.Ui`, and the view one of them produced comes back on `ui`: what kind of thing it is, a title, markdown text that always works, an optional component key, and a `resume` schema saying what has to be collected before the bookmark can be answered. A bookmark whose provider offers no `resume` — a delay, typically — is refused if a caller tries to answer it anyway, and answers that do not fit the schema come back as sentences without ever reaching the workflow.

**Breaking change for existing clients.** When a real provider describes a bookmark, `bookmarks[].payload` and `bookmarks[].activityState` come back **null**. They only duplicated what `ui` now says and they were the two unbounded fields in the response. A bookmark no provider recognised is described by the fallback and keeps both, so an unmapped bookmark loses nothing. There is deliberately no option to restore the old shape: a client that read `payload` should read `ui` instead.

A successful resume may also carry `notes`: the validator's non-fatal sentences, such as a supplied field the task never asked for and that was therefore left out.

### Built-in tools

| Tool | Description |
|------|-------------|
| `search_workflow_instances` | Searches workflow instances by definition, name, correlation id, search term, status and sub status; returns the id, status and incident count per instance |
| `get_my_pending_tasks` | The instances that are running **and** suspended: the work that is waiting for an answer |
| `get_my_running_tasks` | The instances that are running, executing as well as suspended: the work that has not finished |
| `get_my_completed_tasks` | The instances that are finished, including the cancelled and faulted ones; the sub status of a result tells them apart |

The three task tools are the same search behind a fixed status filter, so a caller does not have to know the status names to ask for open or finished work. Each takes an optional `searchTerm` and the same `skip` and `maxResults` paging as the search tool, and returns the same result shape.

All three leave the instances of **system workflows** out. A system workflow is the host's own housekeeping rather than work anyone has to answer, so counting it as a task buries the tasks that are one. `search_workflow_instances` is untouched and still returns them.

Set `IncludeWorkflowInstanceTools` to `false` to leave all four out. Out of the box the three task tools are **not** scoped to the caller, but a host can make them so - see [Scoping the task tools to the caller](#scoping-the-task-tools-to-the-caller).

### Scoping the task tools to the caller

The three `get_my_*_tasks` tools read through `ICallerTaskSearch`, which takes a `CallerTaskCriteria` (search term, status, sub status, definition ids, and whether the system-workflow instances count), an optional order (see [Ordering](#ordering)) and answers a page of instance summaries. Read the system flag through `IsSystemFilter` rather than off the record: it answers `false` while they are left out and `null` once they are asked for, because including them widens the search to every instance rather than narrowing it to the system ones.

The default registration, `UnscopedCallerTaskSearch`, filters on those criteria and nothing else: it returns every matching instance regardless of who started it. That is the only honest default for a generic package - Elsa's own instance filter has no notion of an owner or an assignee, so this package has nothing to narrow on.

A host that does know who is calling replaces the service:

```csharp
services.Replace(ServiceDescriptor.Scoped<ICallerTaskSearch, MyScopedCallerTaskSearch>());
```

`Replace`, not `Add` or `TryAdd`. `McpServerFeature` has already filled the slot with the default, so a `TryAdd` stands down and leaves every caller seeing everything, and an `Add` leaves the winner up to registration order - both fail silently, as too many rows rather than an error.

The seam takes criteria rather than a `WorkflowInstanceFilter` on purpose: a host that scopes by owner has to build a filter type of its own, and copying an incoming filter field by field would quietly drop whatever field Elsa adds next.

#### Ordering

`FindAsync` also takes an optional `CallerTaskOrder` — a `CallerTaskOrderField` (`CreatedAt`, `UpdatedAt`, `FinishedAt`, `Name`, `Status`, `SubStatus`) and a direction. The three tools pass `null`, because they offer no way to ask for one; a host that puts its own HTTP surface on top of this seam — a sortable task list, say — passes what its caller asked for. `null` means the store's own order, which for both Elsa's instance store and an EF Core one derived from it is oldest created first.

An implementation does not map the field to a column itself. `CallerTaskOrdering.SummarizeAsync` does, and it needs one small type from the host to reach its store, because the store call is generic in the order's key type:

```csharp
private sealed class StoreSummarizer(IWorkflowInstanceStore store, WorkflowInstanceFilter filter, PageArgs pageArgs) : ICallerTaskSummarizer
{
    public ValueTask<Page<WorkflowInstanceSummary>> SummarizeAsync<TOrderBy>(WorkflowInstanceOrder<TOrderBy> order, CancellationToken cancellationToken) =>
        store.SummarizeManyAsync(filter, pageArgs, order, cancellationToken);
}
```

Worth going through rather than switching on the field in the host: `FinishedAt` has to be ordered as a `DateTimeOffset?` and the two statuses as their enum type, and getting one of those wrong compiles. The field set is deliberately smaller than what `WorkflowInstanceSummary` carries — a column that cannot be ordered honestly is left out, notably the workflow's name, which lives on the definition rather than on the instance.

The order is a hint about presentation, not about which tasks the answer holds, which is why it is a separate argument rather than a field on the criteria. Note that ordering on a column whose values repeat leaves the order of the tied rows to the database, and skip-based paging over such an order can repeat or miss a row between two pages. Elsa's own instance order has no way to name a tie-breaker, so neither does this.

`search_workflow_instances` deliberately does **not** go through this seam. It is the explicit "search everything" tool; narrowing it would make its name a lie in the other direction. Leave it out with `IncludeWorkflowInstanceTools` if a caller should not have it.

## Examples

Start a workflow, then answer the task it suspended on:

```jsonc
// tools/call
{
  "name": "order-intake",
  "arguments": { "customerId": "1234", "amount": 99.95 }
}
// -> { "workflowInstanceId": "8f2e...", "subStatus": "Suspended", "bookmarkId": "b41c..." }

// tools/call
{
  "name": "order-intake",
  "arguments": { "workflowInstanceId": "8f2e...", "bookmarkId": "b41c...", "approved": true }
}
```

Start a long-running workflow without waiting for it:

```jsonc
{
  "name": "nightly-reconciliation",
  "arguments": { "dispatchWorkflow": true }
}
```

## Limitations

- A tool name is derived from an editable field, so renaming a published workflow renames its tool. The definition id keeps resolving, and is what a client should pin to if it hard-codes a name.
- **Nothing is scoped to the calling identity by default.** A caller that may reach the endpoint may list and start every exposed workflow, and out of the box the instance tools return every instance matching their filter, regardless of who started it. Per-caller and per-tenant restrictions come from the host's authentication, authorization policy and tenancy setup - and, for the three task tools, from replacing `ICallerTaskSearch`. Until a host does that, the `my` in their names is what the caller asks for, not what this package enforces: Elsa's instance filter has no notion of an owner or an assignee. `search_workflow_instances` stays unscoped either way.
- A resume call is checked against the workflow behind the tool - resuming an instance of another workflow is refused - but not against who started that instance.
- Input arguments are also mirrored onto the workflow instance properties, so activities reading from either place see the same call. The caller context properties are excluded from that mirror, so arguments cannot overwrite them.
- A failed tool call returns a generic message; the exception stays in the server log.
- The tool list is not cached; a `tools/list` call queries the workflow definition store.

## Troubleshooting

- **`tools/list` returns only the built-in tools**  
  The workflows are either not published, or not opted in. Check the `mcp:enabled` custom property on the published version, or set `ExposeAllPublishedWorkflows` while diagnosing.

- **`Tool '...' is not available.`**  
  The name is neither a published, opted-in workflow's definition id nor its derived tool name. A derived name follows the workflow's name, so check that the name still reads the way the caller learned it — and that the workflow is published and opted in.

- **401 Unauthorized on the MCP endpoint**  
  The endpoints require an authenticated caller by default. Verify the host's authentication, and that the configured `AuthorizationPolicy` exists.

- **The client does not offer to sign in**  
  Check that the 401 carries `resource_metadata` in its `WWW-Authenticate` header. When it does not, no authorization server is configured. When it does but a valid token is still refused, `AuthenticationScheme` does not name the scheme that validates the token.

- **The workflow starts but does not see its input**  
  Input is passed by argument name. Check that the argument names match the inputs declared on the workflow definition, and that they do not collide with a reserved argument name.

## Planned Features

- [ ] Tool name and description overrides through workflow definition custom properties
- [ ] MCP resources and prompts on top of workflow definitions
- [ ] Optional caching of the tool list, invalidated on publish

## References

- [Model Context Protocol](https://modelcontextprotocol.io/)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Elsa Workflows](https://github.com/elsa-workflows/elsa-core)
