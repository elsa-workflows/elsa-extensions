using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Searches for files or folders in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Searches for files or folders in OneDrive.", Kind = ActivityKind.Task)]
public class SearchFilesOrFolders : OneDriveActivity<IEnumerable<DriveItem>>
{
    /// <summary>
    /// The search query to use.
    /// </summary>
    [Input(Description = "The search query to use.")]
    public Input<string> SearchTerm { get; set; } = default!;

    /// <summary>
    /// The ID of the drive to search in.
    /// </summary>
    [Input(Description = "The ID of the drive to search in. If not specified, the user's default drive will be used.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// The ID of the folder to search within. If not specified, the entire drive will be searched.
    /// </summary>
    [Input(Description = "The ID of the folder to search within. If not specified, the entire drive will be searched.")]
    public Input<string>? FolderId { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var searchTerm = SearchTerm.Get(context);
        var driveId = DriveId?.Get(context);
        var folderId = FolderId?.Get(context);

        DriveItemCollectionResponse searchResults;
        
        if (driveId != null)
        {
            if (folderId != null)
            {
                // Search within a specific folder in a specific drive
                searchResults = await graphClient.Drives[driveId].Items[folderId].Search.GetAsync(
                    requestConfiguration => requestConfiguration.QueryParameters.Q = searchTerm,
                    cancellationToken: context.CancellationToken);
            }
            else
            {
                // Search within a specific drive
                searchResults = await graphClient.Drives[driveId].Root.Search.GetAsync(
                    requestConfiguration => requestConfiguration.QueryParameters.Q = searchTerm,
                    cancellationToken: context.CancellationToken);
            }
        }
        else if (folderId != null)
        {
            // Search within a specific folder in the default drive
            searchResults = await graphClient.Me.Drive.Items[folderId].Search.GetAsync(
                requestConfiguration => requestConfiguration.QueryParameters.Q = searchTerm,
                cancellationToken: context.CancellationToken);
        }
        else
        {
            // Search within the default drive
            searchResults = await graphClient.Me.Drive.Root.Search.GetAsync(
                requestConfiguration => requestConfiguration.QueryParameters.Q = searchTerm,
                cancellationToken: context.CancellationToken);
        }

        Result.Set(context, searchResults.Value ?? new List<DriveItem>());
    }
}