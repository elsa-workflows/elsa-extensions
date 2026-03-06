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
- Same structure and patterns as Elsa.DevOps.GitHub for consistency

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

Pass both values to each activity and store the token securely using the Elsa Secrets management system.

## Activities

### Repositories

| Activity      | Description                          |
|---------------|--------------------------------------|
| GetRepository | Retrieves details of a Git repository |
| GetBranch     | Retrieves details of a specific branch |
| ListBranches  | Lists branches in a repository      |

### Pull Requests

| Activity          | Description                        |
|-------------------|------------------------------------|
| GetPullRequest    | Retrieves details of a pull request |
| CreatePullRequest | Creates a new pull request         |
| ListPullRequests  | Lists pull requests in a repository |

### Work Items

| Activity       | Description                                  |
|----------------|----------------------------------------------|
| GetWorkItem    | Retrieves a work item by ID                  |
| CreateWorkItem | Creates a new work item (Task, Bug, etc.)   |
| UpdateWorkItem | Updates an existing work item               |
| QueryWorkItems | Queries work items using WIQL                |

### Builds

| Activity   | Description                    |
|------------|--------------------------------|
| GetBuild   | Retrieves a build by ID        |
| ListBuilds | Lists builds in a project     |
| QueueBuild | Queues a new build            |

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

- Event-driven triggers (e.g. when a PR is updated) would require Azure DevOps Service Hooks or webhooks and are not implemented in this release.
- The same connection works for Azure DevOps Services (dev.azure.com) and Azure DevOps Server (on-prem); use the appropriate organization URL.

## Troubleshooting

- **Precondition Failed / activity stays Pending**  
  Check the execution log for the activity; it will contain the validation message (e.g. missing Organization URL, invalid Token, or activity-specific required inputs). Supply the required inputs and ensure Organization URL is a valid HTTP/HTTPS URL.

- **401 Unauthorized or authentication errors**  
  Verify that your PAT is valid, not expired, and has the required scopes for the operation. Ensure the organization URL matches your Azure DevOps account (e.g. `https://dev.azure.com/yourorg`).

- **Activity not found in designer**  
  Ensure the extension is registered with `elsa.UseAzureDevOps()` in your Elsa configuration.

- **For production**  
  Store the PAT using Elsa's secret management and reference it from activities instead of hardcoding.

## Planned Features

- [ ] Event-driven triggers (e.g. webhooks / Service Hooks for PR or work item events)
- [ ] Optional appsettings-based default organization URL (with override per activity)

## References

- [Azure DevOps REST API](https://learn.microsoft.com/en-us/rest/api/azure/devops/)
- [.NET client libraries for Azure DevOps](https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/dotnet-client-libraries)
