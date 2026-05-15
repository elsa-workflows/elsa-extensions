using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Searches for SharePoint sites.
/// </summary>
[Activity("Elsa", "OneDrive", "Searches for SharePoint sites.", Kind = ActivityKind.Task)]
public class SearchSites : OneDriveActivity<IEnumerable<Site>>
{
    /// <summary>
    /// The search query to use.
    /// </summary>
    [Input(Description = "The search query to use.")]
    public Input<string> SearchTerm { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var searchTerm = SearchTerm.Get(context);

        // Search for sites
        var searchResults = await graphClient.Sites.GetAsync(
            requestConfiguration => requestConfiguration.QueryParameters.Search = searchTerm,
            cancellationToken: context.CancellationToken);

        Result.Set(context, searchResults?.Value ?? new List<Site>());
    }
}