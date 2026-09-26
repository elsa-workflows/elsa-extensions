# Elsa.DevOps.AzureDevOps Extension

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
    <li><a href="#activities">Activities</a></li>
    <li><a href="#examples">Examples</a></li>
    <li><a href="#limitations">Limitations</a></li>
    <li><a href="#troubleshooting">Troubleshooting</a></li>
    <li><a href="#planned-features">Planned Features</a></li>
    <li><a href="#references">References</a></li>
  </ol>
</details>

## Overview

This package extends [Elsa Workflows](https://github.com/elsa-workflows/elsa-core) with support for **Azure DevOps**. It introduces custom activities that integrate Azure DevOps Git repositories, pull requests, work items, and builds into your workflow logic.

## Features

- Authentication via organization URL and personal access token (PAT)
- Activities for Azure DevOps repositories, pull requests, work items, and builds
- Input validation via Elsa's `CanExecuteAsync` (activities report clear precondition failures when inputs are missing or invalid)
- Elsa trigger support for Azure DevOps Service Hook events
- The Azure DevOps WebApi result types registered as Elsa variable types, so activity results can be stored in typed variables
- Same structure and patterns as Elsa.DevOps.GitHub for consistency
- `IWorkItemEditor`, which reads a work item and writes single fields and comments without going through an activity.
  Behind an interface because the WebApi client cannot be faked, and so its callers can be tested without Azure DevOps.
  `Elsa.DevOps.AzureDevOps.AgentTools` is what puts it in front of an AI agent.
- `WorkItemReader`, the read half of those tools with the rules already applied: the tag filter compared whole, the row
  count bounded, and what Azure DevOps refused turned into a sentence. Every adapter that offers these reads — an agent
  tool set, a host's MCP tools — shares it instead of keeping its own copy.

## Getting Started

### Prerequisites

- Elsa Workflows (e.g. V3) installed in your project
- An Azure DevOps organization (Services or Server) and a personal access token with the required scopes

### Installation

Add the Elsa Azure DevOps extension to your project:

```bash
dotnet add package Elsa.DevOps.AzureDevOps
```

### Configuration

Register the Azure DevOps extension in your Elsa builder (e.g. in `Program.cs` or `Startup.cs`):

```csharp
services
    .AddElsa(elsa =>
    {
        elsa.UseAzureDevOps();
        // Other Elsa configurations...
    });
```

## Authentication

Azure DevOps activities require:

- **Organization URL**: The base URL of your organization (e.g. `https://dev.azure.com/myorg`).
- **Personal access token (PAT)**: Create a token in Azure DevOps under User settings > Personal access tokens. Grant the token the scopes needed for the activities you use (e.g. Code (Read & write) for Repos and PRs, Work Items (Read & write) for work items, Build (Read & execute) for builds).

Both can be left empty on the activity, in which case they fall back to the PAT of the user the workflow runs for and then to the configured defaults (see [Default organization, project and token](#default-organization-project-and-token)). Store the token in the Elsa Secrets management system rather than in configuration.

## Activities

### Repositories

| Activity      | Description                          |
|---------------|--------------------------------------|
| GetRepository | Retrieves details of a Git repository |
| GetBranch     | Retrieves details of a specific branch |
| ListBranches  | Lists branches in a repository      |
| DisplayRepository | Shows a repository read-only and waits until it has been seen |

### Pull Requests

| Activity          | Description                        |
|-------------------|------------------------------------|
| GetPullRequest    | Retrieves details of a pull request |
| CreatePullRequest | Creates a new pull request         |
| ListPullRequests  | Lists pull requests in a repository |
| DisplayPullRequest | Shows a pull request read-only and waits until it has been seen |

### Work Items

| Activity           | Description                                       |
|--------------------|---------------------------------------------------|
| GetWorkItem        | Retrieves a work item by ID                       |
| CreateWorkItem     | Creates a new work item (Task, Bug, etc.), tags and other fields included |
| UpdateWorkItem     | Updates an existing work item                     |
| AddWorkItemComment | Adds a comment to a work item's discussion        |
| AddWorkItemRelation | Links a work item to another work item (Parent, Child, Related, ...) |
| AddWorkItemHyperlink | Attaches a URL to a work item                   |
| QueryWorkItems     | Queries work items using WIQL                     |
| DisplayWorkItem    | Shows a work item read-only and waits until it has been seen |

#### Showing a work item to a person

`DisplayWorkItem` is a UI interaction: it suspends the workflow on a bookmark and waits, the way `UIInteraction` does, but with a display target of its own - `WorkItem` - rather than one of the four in the shared `UiDisplayTarget` enum, which lives in a binary drop this repository cannot extend. It reads nothing from Azure DevOps; hand it the work item that `GetWorkItem` or a work item trigger produced.

The bookmark payload is a `WorkItemDisplayBookmark` (kind `azuredevops-workitem/v1`) carrying the display target, a display id and the work item id. The work item itself is projected to a `WorkItemSnapshot` and written to the activity state as JSON, under `WorkItemDisplay`, joined to the bookmark on `WorkItemDisplayId`. That is the contract a viewer reads: a host's Studio renders it as a read-only card with a link into Azure DevOps, and resumes the bookmark once the reader confirms.

#### Setting fields that have no dedicated input

`CreateWorkItem` and `UpdateWorkItem` expose **Title** and **Description** as their own inputs, and `CreateWorkItem` also has **Tags**. Every other field goes through the **Fields** input, a key/value editor where the key is the field's reference name and the value is what to write:

| Key | Value |
|-----|-------|
| `System.State` | `Active` |
| `System.AreaPath` | `Contoso\Web` |
| `Microsoft.VSTS.Common.Priority` | `2` |
| `Custom.Afdeling` | `Thuiszorg` |

Reference names are what the API expects, not the display labels shown in Azure DevOps. Look one up under **Project settings > Process > _work item type_ > _field_**, or read it off the `Fields` dictionary of a `WorkItem` returned by `GetWorkItem`. A custom field added through an inherited process is usually named `Custom.<FieldName>`.

Each value can be a literal or an expression, so a field can be filled from a variable or from an earlier activity's output. Fields left out of the dictionary are not touched.

A field that a dedicated input already covers is skipped with a log entry: Azure DevOps rejects a patch document with two operations on the same path, so `Title`, `Description` and `Tags` win over a `Fields` entry for the same reference name.

Tags have their own input on `CreateWorkItem` rather than only living in `Fields`, because a code-defined workflow can bind a dedicated input to a variable and Elsa Studio then shows that binding on the node — the dictionary editor cannot. `System.Tags` is a single field holding a `;`-separated list, so writing it replaces the whole set rather than adding to it.

#### Comments

A comment is not a work item field, so `UpdateWorkItem` cannot set one — comments live behind their own endpoint. Use `AddWorkItemComment`, which takes the work item ID, the comment text and the format (Markdown or HTML), and outputs the created `Comment`.

#### Relations

A link between two work items is not a field either: it is an operation on `/relations/-` whose value is an object rather than a scalar, so the `Fields` dictionary cannot express it. `AddWorkItemRelation` takes the work item ID, the target work item ID, the link type and an optional comment, and outputs the updated `WorkItem`.

**Link type** is a dropdown, and each value names the role the *target* plays — the same wording the work item form uses. Adding **Parent** on work item 1234 pointing at 5678 makes 5678 the parent of 1234:

| Link type | Reference name |
|-----------|----------------|
| Related | `System.LinkTypes.Related` |
| Parent | `System.LinkTypes.Hierarchy-Reverse` |
| Child | `System.LinkTypes.Hierarchy-Forward` |
| Predecessor | `System.LinkTypes.Dependency-Reverse` |
| Successor | `System.LinkTypes.Dependency-Forward` |
| Duplicate | `System.LinkTypes.Duplicate-Forward` |
| Duplicate Of | `System.LinkTypes.Duplicate-Reverse` |
| Tested By | `Microsoft.VSTS.Common.TestedBy-Forward` |
| Tests | `Microsoft.VSTS.Common.TestedBy-Reverse` |
| Affects | `Microsoft.VSTS.Common.Affects-Forward` |
| Affected By | `Microsoft.VSTS.Common.Affects-Reverse` |

The reference names say `Forward` or `Reverse`, which reads as the opposite of the label for hierarchy and dependency links; the dropdown exists so nobody has to keep that straight. **Affects** and **Affected By** exist only in the CMMI process — on Agile and Scrum, Azure DevOps rejects them.

`AddWorkItemHyperlink` attaches a plain URL instead of a work item, taking the work item ID, the URL and an optional comment. It is a separate activity because it needs no link type, where `AddWorkItemRelation` needs no URL.

Both activities are **idempotent**: they read the work item's existing relations first and, when the link is already there, log an `Info` entry and complete without patching. A workflow that runs again, or an Elsa retry after a timeout, therefore does not fault on a duplicate relation. Neither activity removes or replaces relations, and neither creates artifact links to commits, pull requests or builds — those need a `vstfs:///` URI rather than an HTTP URL.

### Builds

| Activity   | Description                    |
|------------|--------------------------------|
| GetBuild   | Retrieves a build by ID        |
| ListBuilds | Lists builds in a project     |
| QueueBuild | Queues a new build            |
| DisplayBuild | Shows a build read-only and waits until it has been seen |

#### Showing a build, a pull request or a repository to a person

`DisplayBuild`, `DisplayPullRequest` and `DisplayRepository` are the same kind of UI interaction as `DisplayWorkItem`, and for the same reason: they suspend the workflow on a bookmark and wait, with a display target of their own - `Build`, `PullRequest`, `Repository` - rather than one of the four in the shared `UiDisplayTarget` enum, which lives in a binary drop this repository cannot extend. None of them reads anything from Azure DevOps, so none of them needs a PAT; hand them what `GetBuild`, `GetPullRequest`, `GetRepository` or a trigger produced.

The three share one bookmark payload, `DevOpsDisplayBookmark`, carrying the kind (`azuredevops-build/v1`, `azuredevops-pullrequest/v1`, `azuredevops-repository/v1`), the display target, a display id and the resource id as text - text because a repository is identified by a GUID. Because the payload is shared, a viewer **must** guard on the kind: every member it reads is present on all three. What is on show is projected to a `BuildSnapshot`, `PullRequestSnapshot` or `RepositorySnapshot` and written to the activity state as JSON under `BuildDisplay`, `PullRequestDisplay` or `RepositoryDisplay`, joined to the bookmark on the matching `*DisplayId`. a host's Studio renders each as a read-only card with a link into Azure DevOps, and resumes the bookmark once the reader confirms.

`DisplayWorkItem` deliberately stays on its own `WorkItemDisplayBookmark`: it shipped first, its payload names the work item id outright, and rewriting a wire format that suspended instances are already waiting on would strand them.

### Variable types

The Azure DevOps WebApi types the activities produce are registered as Elsa variable types under the category **Azure DevOps**, so a workflow can keep an activity result in a typed variable and address its properties from an expression:

| Type | Produced by |
|------|-------------|
| `WorkItem`, `ICollection<WorkItem>` | GetWorkItem, CreateWorkItem, UpdateWorkItem, AddWorkItemRelation, AddWorkItemHyperlink, QueryWorkItems |
| `Comment` | AddWorkItemComment |
| `PostedComment` | Work Item Commented |
| `Build`, `ICollection<Build>` | GetBuild, QueueBuild, ListBuilds |
| `GitPullRequest`, `ICollection<GitPullRequest>` | GetPullRequest, CreatePullRequest, ListPullRequests |
| `GitRepository` | GetRepository |
| `GitBranchStats`, `ICollection<GitBranchStats>` | GetBranch, ListBranches |

Registration happens in `AzureDevOpsFeature`, so `elsa.UseAzureDevOps()` is all that is needed. Without it these values can only travel as untyped objects.

### Triggers

The extension exposes one trigger component per Azure DevOps Service Hook event. Triggers are shown in the existing Azure DevOps toolbox groups:

| Trigger | Event type |
|------------|-------------|
| Build Completed | `build.complete` |
| Build In Progress | `build.inProgress` |
| Build Queued | `build.queued` |
| Code Pushed | `git.push` |
| Pull Request Created | `git.pullrequest.created` |
| Pull Request Updated | `git.pullrequest.updated` |
| Pull Request Merged | `git.pullrequest.merged` |
| Work Item Created | `workitem.created` |
| Work Item Updated | `workitem.updated` |
| Work Item Deleted | `workitem.deleted` |
| Work Item Commented | `workitem.commented` as delivered, indexed as `workitem.commentedOn` |

Only the last row has two names, and it is worth knowing which is which. Azure DevOps delivers `workitem.commented`; this package indexes the trigger as `workitem.commentedOn`, and normalizes the delivered name to it on the way in. The indexed name stays as it is because published triggers and suspended instances already carry it, and renaming it would strand both until every workflow was republished.

#### Pull Request Updated and Merged

Both take an optional **Pull Request Id**. Left empty, the trigger fires for every pull request in the project; filled in, only for that one.

It is worth filling in whenever the workflow already knows which pull request it is about — one it created itself, say. The filter lives in the bookmark, which is what an arriving delivery is matched against, so it decides *whether the workflow wakes up at all*. Filtering after the trigger instead does not work: by then the trigger has completed, and a completed trigger is not a state a workflow can go back to waiting from, so the instance carries on with somebody else's pull request.

Pull request IDs are allocated per organization, and a trigger is keyed on the project, so the pair identifies one pull request for every host that works in a single organization. A host that polls several (see [Polling](#polling)) can hold two pull requests with the same number in two organizations; a trigger filtered on that number listens to both.

Pull Request Created has no such input on purpose: a workflow cannot know the id of a pull request that does not exist yet.

#### Work item trigger outputs

The four work item triggers all expose the work item twice, plus its state as a plain string:

| Output | Description |
|--------|-------------|
| `Work Item` | The work item the event is about. Bind this one. |
| `Result` | The same value. It comes from the base activity and cannot be renamed without orphaning every workflow definition already bound to it, so it keeps being set. |
| `State` | The state of the work item, for example `Active` or `Closed`. Empty when the event carried none. A separate output because a workflow binding cannot navigate into the work item's field dictionary, so a decision that waits for a state needs it as a value. |

#### Work Item Commented

Besides the work item, this trigger hands over the comment that caused the event:

| Output | Description |
|--------|-------------|
| `Comment Text` | The text of the comment, as HTML. Empty when the event carried none. A separate output because matching on what was said — a mention, a keyword, a command — is the common case, and a plain string is what a flow decision works with most easily. |
| `Comment` | A `PostedComment` with the text, the author and the timestamp. |

`PostedComment` is this extension's own type rather than the WebApi `Comment`, because the two sources know different amounts. The poller has read the comment through the API and fills in everything, its id included. A Service Hook does not send a comment at all: it sends the work item with the text in `System.History`, so the author and timestamp are recovered from the payload and `Id` stays empty. Handing out a `Comment` with a zero id would suggest something that could be fetched or replied to.

Two things about the Service Hook path that used to make the work item unusable there, and no longer do. The payload is read with case-insensitive options, because a Service Hook resource is camelCase and the WebApi models are PascalCase; with the defaults nothing bound at all and the workflow received a work item with no id and no fields. And a `workitem.updated` delivery describes the *update* rather than the work item — its `id` is the revision number and its `fields` hold only what changed, as `oldValue`/`newValue` pairs — so the work item is taken from the `revision` it carries, falling back to `workItemId` when a delivery arrives without one.

Neither applied to the poller, which hands over a work item it read through the API. That is why both went unnoticed until a Service Hook delivery first reached a workflow, and why the symptom was an activity downstream refusing a work item id of zero rather than anything failing at the trigger.

Every trigger requires a project, given as either its ID or its name. Leaving it empty falls back to the default project from configuration (see [Default organization, project and token](#default-organization-project-and-token)); publishing a workflow whose trigger has neither fails. Optionally, the work item triggers can be restricted to a work item type, and Work Item Updated, Deleted and Commented also to a work item ID; Pull Request Updated and Merged can be restricted to a pull request ID. The extension exposes the endpoint `POST /webhooks/azure-devops`. Configure Basic Authentication in the Azure DevOps Service Hook and store the matching credentials in one Elsa Secret named `AzureDevOps:WebhookBasicAuth`, with a JSON value such as `{"username":"azure-devops","password":"change-me"}`. After validation, the request is converted to an `AzureDevOpsWebhookEvent` and routed to the matching trigger.

### Default organization, project and token

The `OrganizationUrl`, `Project` and `Token` inputs each fall back to a single configured default when left empty, so a connection only has to be described once per environment:

```csharp
services
    .AddElsa(elsa =>
    {
        elsa.UseAzureDevOps(feature =>
        {
            feature.ConfigureOptions = options =>
            {
                options.DefaultOrganizationUrl = "https://dev.azure.com/myorg";
                options.DefaultProject = "MyProject";
                options.DefaultTokenSecretName = "AzureDevOps:Pat";
                // The per-user secret an activity prefers over the default; this is the default value.
                options.UserTokenSecretNameFormat = "AzureDevOps:{user}:Pat";
            };
        });
    });
```

Or, bound from configuration:

```json
{
  "AzureDevOps": {
    "DefaultOrganizationUrl": "https://dev.azure.com/myorg",
    "DefaultProject": "MyProject",
    "DefaultTokenSecretName": "AzureDevOps:Pat",
    "UserTokenSecretNameFormat": "AzureDevOps:{user}:Pat"
  }
}
```

An activity's token resolves in order of preference:

1. The activity's own `Token` input.
2. The Elsa Secret holding the PAT of the user the workflow runs for, named after `UserTokenSecretNameFormat` (default `AzureDevOps:{user}:Pat`).
3. `DefaultToken`, which puts the PAT in plain configuration and is meant for local development only.
4. The Elsa Secret named by `DefaultTokenSecretName`.

Step 2 is what lets each person act under their own identity: a workflow started by `alice@contoso.com` reads the secret `AzureDevOps:alice:Pat`, so the work items it creates, edits and comments on are attributed to Alice rather than to a shared service identity. The user is taken from the sign-in name on the ambient HTTP request (`preferred_username`, `upn`, `unique_name` or the email claim) and, for an activity that resumes after a suspension and so has no request to read, from the caller recorded on the workflow instance — the MCP extension writes it there when `Mcp:PropagateCallerContext` is on. Everything up to the `@`, minus any `DOMAIN\` prefix, becomes `{user}`. A display name such as `Alice Anderson` is not turned into an account name, so it is treated as no user rather than guessed at. Setting `UserTokenSecretNameFormat` to an empty value switches the per-user lookup off.

A workflow that no user started — one run by the scheduler, or by a webhook — has no user to name a secret after and so falls through to `DefaultToken` / `DefaultTokenSecretName`. Point `DefaultTokenSecretName` at a shared PAT if those workflows need to reach Azure DevOps.

A caller with no workflow instance behind it at all — a tool called over MCP, which runs inside the caller's own request — resolves its token through `AzureDevOpsTokenResolver.ResolveForCallerAsync` instead. Same list, minus step 1 and minus the fallback to the instance: the claims of the request, then the configured defaults. Both paths end at the same `AzureDevOps:{user}:Pat`, so the same person is the same person either way, and a caller who stored no PAT reads under the host's token — which for a read costs attribution rather than correctness, but does mean they may be shown a work item they could not open themselves.

### Polling

In addition to webhook delivery, the extension can poll Azure DevOps, so an environment that Service Hooks cannot
reach still fires the same Elsa triggers. Polling is split into families, each with its own system workflow, its own
schedule and its own checkpoint, so one family can lag or fail without affecting the others:

| Family | Section | Events |
|--------|---------|--------|
| Work items | `AzureDevOps:Polling:WorkItems` | `workitem.created`, `workitem.updated`, `workitem.commentedOn`, `workitem.deleted` |
| Builds | `AzureDevOps:Polling:Builds` | `build.queued`, `build.inProgress`, `build.complete` |
| Pull requests | `AzureDevOps:Polling:PullRequests` | `git.pullrequest.created`, `git.pullrequest.updated`, `git.pullrequest.merged` |
| Pushes | `AzureDevOps:Polling:Pushes` | `git.push` |

Azure DevOps offers no way to query when a pull request last changed, so `git.pullrequest.updated` cannot come out of
a poll. Instead, the pull request poller starts a watcher workflow per newly created pull request — one instance,
correlated on the pull request ID — that re-reads it every `PullRequests:WatchInterval` (defaulting to the interval of
the family) and fires the Pull Request Updated trigger when its title, description, status, merge status, target
branch, draft flag, reviewer votes or newest iteration changed. The watcher ends itself as soon as the pull request is
no longer active — including at the very first read, which records the baseline: a pull request that is already closed
by then finishes the instance on the spot rather than waiting out an interval to learn the same thing.

Two consequences are worth knowing. Pull requests that were already open when polling was switched on get no watcher,
because no creation event is ever seen for them. And a pull request announced over a Service Hook gets none either, by
design: an environment with working Service Hooks receives real update events already. Set `PullRequests:WatchUpdates`
to `false` to switch watching off entirely, leaving created and merged working.

Configure it when registering the feature:

```csharp
services
    .AddElsa(elsa =>
    {
        elsa.UseAzureDevOps(feature =>
        {
            feature.ConfigurePollingOptions = options =>
            {
                options.Enabled = true;
                options.Interval = TimeSpan.FromMinutes(5);
                // The PAT every family polls under. Configuring it here rather than as DefaultTokenSecretName keeps
                // it out of reach of activities, which authenticate as the user the workflow runs for; a family only
                // names one of its own when it polls something this PAT may not read.
                options.TokenSecretName = "AzureDevOps:PollingPat";
                // OrganizationUrl and Project only when polling targets something other than the configured defaults.
            };
        });
    });
```

Or, bound from configuration:

```json
{
  "AzureDevOps": {
    "Polling": {
      "Enabled": true,
      "Interval": "00:05:00",
      "TokenSecretName": "AzureDevOps:PollingPat",
      "WorkItems": { "Enabled": true },
      "Builds": { "Enabled": true },
      "PullRequests": { "Enabled": true },
      "Pushes": { "Enabled": true, "Repositories": [] }
    }
  }
}
```

`Polling:Enabled` is the master switch: no poll runs while it is off, whoever asked for it. Underneath it each family
is on by default and can be switched off on its own, and that family switch means one thing only — whether the host
starts that family's polling workflow when it starts. It does not stop a poll that is asked for anyway: the manual
event on the built-in workflow still polls, a workflow of your own carrying the poll activity still polls, and an
instance that was already running when the switch went off keeps polling on its timer until it is stopped. Switching a
family off in a host that has already polled therefore means two things: deploy the setting, and stop that family's
instance (its ID is the definition ID, e.g. `AzureDevOpsWorkItemPollingWorkflow`). To stop polling outright without
touching instances, use the master switch. Every family section accepts `Enabled`, `Interval`, `OrganizationUrl`,
`Project`, `Token`, `TokenSecretName`, `Top` and `LookbackWindow`; `Interval` falls back to `Polling:Interval`,
`Token` and `TokenSecretName` to `Polling:Token` and `Polling:TokenSecretName`, and the rest to the extension-wide
defaults described under
[Default organization, project and token](#default-organization-project-and-token).

A family's token resolves as `<family>:Token`, then `<family>:TokenSecretName`, then `Polling:Token`, then
`Polling:TokenSecretName`, and only then `DefaultToken` / `DefaultTokenSecretName`. Polling runs for no user, so it
never reads a per-user secret. Keeping the polling PAT under `AzureDevOps:Polling` rather than in
`DefaultTokenSecretName` is what keeps activities from authenticating with it.

That the lookup steps over a rung that yields nothing — rather than dropping to the defaults the moment the name it
was given resolves to no secret — is what `Polling:TokenSecretName` is for. Name the polling PAT there and a family
whose own secret was never created, or was removed since, keeps polling as polling. Name it only on the families and
there is nothing between them and `DefaultTokenSecretName`, which is the PAT activities fall back to when no user is
known. Pull requests feel this first: a watcher carries the secret name of the poll that started it on its instance,
so it outlives the configuration that named it.

Every poll writes one line to its journal saying that it ran and how many events it dispatched, at the level
`Polling:Diagnostics:Level` names — `Debug` by default, `None` to write nothing. The level travels as the journal
entry's name, so at `Error` the line reads as an error in Studio, and it is also written to the log, which is what gets
it out of a host that exports nothing below `Warning`. Raise it while you are working out whether a family polls at
all: a poll that found nothing is otherwise indistinguishable from a poll that never happened, because the master
switch, the family switch and a polling workflow that was never started are all silent in exactly the same way.

Push polling reads every repository in the project unless `Pushes:Repositories` names the ones to watch, by name or
by ID. A polled push carries the same camel-cased payload shape as a Service Hook delivery, so a workflow reading
`repository.name` off the Code Pushed trigger works over either route.

Work item polling also reports deletions, by comparing the project's recycle bin with what it held on the previous
poll. The first poll after switching polling on only records the baseline, so an existing recycle bin is not replayed.
Set `WorkItems:IncludeDeleted` to `false` where the polling token may not read the recycle bin; the other three work
item events keep working.

#### Polling something else from a workflow of your own

Each family's configuration describes one poll, and its system workflow runs exactly that one. To poll something else
as well — a second project, another organization, a different set of repositories — put the family's poll activity in a
workflow of your own, give it a timer, and fill in the inputs that have to differ. Every input is optional and falls
back to the family's configuration, which is why the built-in workflows, which fill in none of them, keep behaving
exactly as they always did.

| Input | On | Falls back to |
|-------|----|---------------|
| `Project` | every poll activity | `<family>:Project`, then `DefaultProject` |
| `OrganizationUrl` | every poll activity | `<family>:OrganizationUrl`, then `DefaultOrganizationUrl` |
| `Token` | every poll activity | the configured credential of the family |
| `TokenSecretName` | every poll activity | the configured credential of the family |
| `Top` | every poll activity | `<family>:Top` |
| `LookbackWindow` | every poll activity | `<family>:LookbackWindow` |
| `IncludeDeleted` | Poll Work Item Changes | `WorkItems:IncludeDeleted` |
| `Repositories` | Poll Pushes | `Pushes:Repositories` |
| `WatchUpdates` | Poll Pull Request Changes | `PullRequests:WatchUpdates` |

`Token` and `TokenSecretName` replace the configured credential as a pair, so a configured token is never used beside
a secret name supplied here. Prefer `TokenSecretName`: it keeps the PAT out of the workflow definition, and for pull
requests it is the only credential a watcher can inherit.

Four things stay out of reach of an activity. `Polling:Enabled` remains the operator's switch — a poll activity goes
idle with the rest of the host when polling is switched off, wherever it sits. `<family>:Enabled` is not offered as an
input either, but for the opposite reason: it decides what the host starts and has no say over a poll that is already
running, so overriding it would change nothing. `Interval` belongs to the timer of the workflow the activity sits in,
and in your own workflow that timer is yours. `WatchInterval` is read when the watcher workflow's definition is built
rather than when a poll runs, so an input for it would change nothing.

Checkpoints live on the workflow instance, so your workflow keeps its own and never competes with the built-in one.

**Pull requests across organizations.** A watcher is handed the organization and the secret name the poll ran under, so
it re-reads its pull request where the poll found it. Its instance ID and correlation ID carry that organization as
well, because Azure DevOps allocates pull request IDs per organization and two organizations can hold the same number.
A poll of the host's own organization keeps the plain, historical key, so watchers already running are unaffected.

A poll of another organization that supplies a literal `Token` rather than a `TokenSecretName` starts no watchers, and
says so in the log: a watcher outlives the poll and resolves its own credential, and a token is not something to store
on its instance. Created and merged are reported as usual; only the watched update is given up.

**Upgrading an existing installation.** A running workflow instance keeps executing the definition version it was
created under, so a work item polling instance that already existed before this version goes on polling without ever
reading the recycle bin. Delete the instance `AzureDevOpsWorkItemPollingWorkflow` once after upgrading; the hosted
starter recreates it on the next host start and deletions are reported from then on. New installations need nothing.

`AzureDevOpsPollingEvent`, a public type of this package, changed shape in the same release: its `WorkItemId` and
`WorkItemType` properties became `ResourceId` and `Description`, so that one record can describe a build, a pull
request or a push as well. It only feeds the journal and execution-log payloads of the poll activities, but code that
reads those payloads by property name has to follow.

## Examples

Get a repository:

```csharp
builder
    .StartWith<GetRepository>(activity =>
    {
        activity.OrganizationUrl = new Input<string>("https://dev.azure.com/myorg");
        activity.Token = new Input<string>("your-pat");
        activity.Project = new Input<string>("MyProject");
        activity.RepositoryName = new Input<string>("MyRepo");
    })
    .Then<WriteLine>(activity =>
    {
        activity.Text = new Input<string>(context =>
            $"Repository: {context.Get(RetrievedRepository)?.Name}");
    });
```

Create a work item:

```csharp
builder
    .StartWith<CreateWorkItem>(activity =>
    {
        activity.OrganizationUrl = new Input<string>("https://dev.azure.com/myorg");
        activity.Token = new Input<string>("your-pat");
        activity.Project = new Input<string>("MyProject");
        activity.WorkItemType = new Input<string>("Task");
        activity.Title = new Input<string>("Automatically created task");
        activity.Description = new Input<string>("Created by an Elsa workflow.");
    });
```

## Limitations

- The same connection works for Azure DevOps Services (dev.azure.com) and Azure DevOps Server (on-prem); use the appropriate organization URL.

## Troubleshooting

- **Precondition Failed / the run faults**  
  Check the execution log for the activity; it will contain the validation message (e.g. missing Organization URL, invalid Token, or activity-specific required inputs), and the same message is on the instance's incident. Supply the required inputs and ensure Organization URL is a valid HTTP/HTTPS URL.

  A bad input faults the instance on purpose. Reporting it the other obvious way — returning `false` from `CanExecuteAsync` — leaves the run neither finished nor failed: Elsa skips the activity, a skipped activity never signals completion, and its parent container waits forever with the instance sitting `Running`/`Suspended`. If you see that symptom rather than a fault, an activity is still refusing instead of throwing.

- **401 Unauthorized or authentication errors**  
  Verify that your PAT is valid, not expired, and has the required scopes for the operation. Ensure the organization URL matches your Azure DevOps account (e.g. `https://dev.azure.com/yourorg`).

- **Activity not found in designer**  
  Ensure the extension is registered with `elsa.UseAzureDevOps()` in your Elsa configuration.

- **Every delivery is rejected with `401` although the password is right**  
  The subscription's `Basic authentication username` is empty, so Azure DevOps sends `Basic base64(":password")` and the username half of the comparison fails. Fill the username in on the subscription. Do **not** blank the username in the secret to match: an empty configured username is refused before anything is compared, so it cannot work and it takes down every subscription that was configured correctly at the same time. When the subscription cannot be reached, `AzureDevOps:WebhookAuthentication:PasswordOnly` accepts a delivery on its password alone — a genuine loosening, logged as a warning on every delivery it lets through, and meant to be switched back off.

- **A Service Hook delivery is accepted with `202` but nothing runs**  
  The delivery matched no indexed trigger and no waiting bookmark, which is silent by design — nothing failed, there was simply nothing listening. Switch on the webhook diagnostics to see both sides at once:

  ```jsonc
  "AzureDevOps": {
    "WebhookDiagnostics": {
      // The level every diagnostic line is written at. Raise it above whatever the host's log filter
      // drops, or the lines are produced and then discarded before they leave the process.
      "Level": "Warning",
      // Writes the delivered message as it arrived. A work item payload carries whatever people put in
      // the work item, so switch this off once the question is answered.
      "IncludePayloads": true
    }
  }
  ```

  The endpoint then reports why a delivery was rejected — including whether the sent credentials differ from the secret, compared by fingerprint so neither is written to the log — and the handler reports the bookmarks the event produced next to the triggers and bookmarks the stores actually hold. A project ID on one side against a project name on the other is the usual answer: a work item event only carries the project name in `System.TeamProject`, which Azure DevOps omits unless the subscription is set to send **all** resource details.

- **For production**  
  Store the PAT using Elsa's secret management and reference it from activities instead of hardcoding.

## Planned Features

- [x] Event-driven triggers for Azure DevOps Service Hook events
- [x] Optional appsettings-based default organization URL (with override per activity)

## References

- [Azure DevOps REST API](https://learn.microsoft.com/en-us/rest/api/azure/devops/)
- [.NET client libraries for Azure DevOps](https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/dotnet-client-libraries)
