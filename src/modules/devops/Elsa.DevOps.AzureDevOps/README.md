# Elsa.DevOps.AzureDevOps

This module provides integration with Azure DevOps for Elsa Workflows. It enables workflows to interact with Azure DevOps Git repositories, pull requests, work items, and builds.

## Features

- Authentication via organization URL and personal access token (PAT)
- Activities for Azure DevOps repositories, pull requests, work items, and builds
- Same structure and patterns as Elsa.DevOps.GitHub for consistency

## Getting Started

### Installation

Add the Elsa Azure DevOps extension to your project:

```bash
dotnet add package Elsa.DevOps.AzureDevOps
```

### Registration

Register the Azure DevOps extension in your Elsa builder:

```csharp
// Program.cs or Startup.cs
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

## Available Activities

### Repositories

| Activity | Description |
|----------|-------------|
| GetRepository | Retrieves details of a Git repository |
| GetBranch | Retrieves details of a specific branch |
| ListBranches | Lists branches in a repository |

### Pull Requests

| Activity | Description |
|----------|-------------|
| GetPullRequest | Retrieves details of a pull request |
| CreatePullRequest | Creates a new pull request |
| ListPullRequests | Lists pull requests in a repository |

### Work Items

| Activity | Description |
|----------|-------------|
| GetWorkItem | Retrieves a work item by ID |
| CreateWorkItem | Creates a new work item (Task, Bug, etc.) |
| UpdateWorkItem | Updates an existing work item |
| QueryWorkItems | Queries work items using WIQL |

### Builds

| Activity | Description |
|----------|-------------|
| GetBuild | Retrieves a build by ID |
| ListBuilds | Lists builds in a project |
| QueueBuild | Queues a new build |

## Example Usage

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

## Notes

- Event-driven triggers (e.g. when a PR is updated) would require Azure DevOps Service Hooks or webhooks and are not implemented in this release.
- For production, store the PAT using Elsa's secret management.
- The same connection works for Azure DevOps Services (dev.azure.com) and Azure DevOps Server (on-prem); use the appropriate organization URL.

## References

- [Azure DevOps REST API](https://learn.microsoft.com/en-us/rest/api/azure/devops/)
- [.NET client libraries for Azure DevOps](https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/dotnet-client-libraries)
