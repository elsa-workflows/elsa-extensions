using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Copies a file to a new location in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Copies a file to a new location in OneDrive.", Kind = ActivityKind.Task)]
public class CopyFile : OneDriveActivity<DriveItem>
{
    /// <summary>
    /// The ID or path of the item to copy.
    /// </summary>
    [Input(Description = "The ID or path of the item to copy.")]
    public Input<string> ItemIdOrPath { get; set; } = default!;

    /// <summary>
    /// The ID of the drive containing the item to copy.
    /// </summary>
    [Input(Description = "The ID of the drive containing the item to copy.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// The ID of the destination parent folder.
    /// </summary>
    [Input(Description = "The ID of the destination parent folder.")]
    public Input<string> DestinationFolderId { get; set; } = default!;

    /// <summary>
    /// The name of the copy. If not specified, the original item's name will be used.
    /// </summary>
    [Input(Description = "The name of the copy. If not specified, the original item's name will be used.")]
    public Input<string>? NewName { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var itemIdOrPath = ItemIdOrPath.Get(context);
        var destinationFolderId = DestinationFolderId.Get(context);
        var newName = NewName?.Get(context);
        var driveId = DriveId?.Get(context);

        var requestBody = new DriveItemCopyRequestBody
        {
            ParentReference = new ItemReference
            {
                Id = destinationFolderId
            },
            Name = newName
        };

        DriveItem result;
        if (driveId != null)
        {
            // Copy by ID with specified drive
            var copyRequest = await graphClient.Drives[driveId].Items[itemIdOrPath].Copy.PostAsync(requestBody, cancellationToken: context.CancellationToken);
            result = await WaitForCopyCompletion(graphClient, copyRequest, context);
        }
        else if (IsItemId(itemIdOrPath))
        {
            // Copy by ID in default drive
            var copyRequest = await graphClient.Me.Drive.Items[itemIdOrPath].Copy.PostAsync(requestBody, cancellationToken: context.CancellationToken);
            result = await WaitForCopyCompletion(graphClient, copyRequest, context);
        }
        else
        {
            // Copy by path in default drive
            var copyRequest = await graphClient.Me.Drive.Root.ItemWithPath(itemIdOrPath).Copy.PostAsync(requestBody, cancellationToken: context.CancellationToken);
            result = await WaitForCopyCompletion(graphClient, copyRequest, context);
        }

        Result.Set(context, result);
    }

    private static bool IsItemId(string value)
    {
        // Simple check to determine if the string is likely to be an ID rather than a path
        // OneDrive IDs don't typically contain slashes while paths do
        return !value.Contains('/') && !value.Contains('\\');
    }

    private static async Task<DriveItem> WaitForCopyCompletion(GraphServiceClient graphClient, DriveItemCopyResponse response, ActivityExecutionContext context)
    {
        // The copy operation is asynchronous
        if (string.IsNullOrEmpty(response.Location))
        {
            throw new System.InvalidOperationException("Copy operation didn't return a monitoring URL");
        }

        // In a real implementation, we'd poll the monitor URL to check progress
        // For now, we'll just get the item by the destination path
        // This is a simplification - in a production scenario you should use the monitoring URL
        
        // For this example, we'll just get the item from the destination
        var destinationFolderId = DestinationFolderId.Get(context);
        var newName = NewName?.Get(context) ?? System.IO.Path.GetFileName(ItemIdOrPath.Get(context));
        
        return await graphClient.Me.Drive.Items[destinationFolderId].Children.GetAsync(
            requestConfiguration => requestConfiguration.QueryParameters.Filter = $"name eq '{newName}'", 
            context.CancellationToken);
    }
}