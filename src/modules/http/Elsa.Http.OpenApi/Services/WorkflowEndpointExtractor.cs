using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Bookmarks;
using Elsa.Http.OpenApi.Contracts;
using Elsa.Http.OpenApi.Models;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Common.Models;
using Elsa.Workflows;
using System.Net;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Management.Stores;
using Elsa.Workflows.Models;

namespace Elsa.Http.OpenApi.Services;

/// <summary>
/// Service for extracting HTTP endpoint definitions from Elsa workflows.
/// </summary>
public class WorkflowEndpointExtractor : IWorkflowEndpointExtractor
{
    private readonly ITriggerStore _triggerStore;

    public WorkflowEndpointExtractor(ITriggerStore triggerStore)
    {
        _triggerStore = triggerStore;
    }

    public async Task<List<EndpointDefinition>> ExtractEndpointsAsync(CancellationToken cancellationToken = default)
    {
        var endpoints = new List<EndpointDefinition>();

        var bookmarkName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var triggerFilter = new TriggerFilter
        {
            Name = bookmarkName
        };
        var triggers = (await _triggerStore.FindManyAsync(triggerFilter, cancellationToken)).ToList();

        var triggerName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        var filteredTriggers = triggers.Where(x => x.Name == triggerName && x.Payload != null);

        foreach (var trigger in filteredTriggers)
        {
            var payload = trigger.GetPayload<HttpEndpointBookmarkPayload>();

            endpoints.Add(new EndpointDefinition
            {
                Path = payload.Path,
                Method = payload.Method
            });
        }

        return endpoints;
    }
}
