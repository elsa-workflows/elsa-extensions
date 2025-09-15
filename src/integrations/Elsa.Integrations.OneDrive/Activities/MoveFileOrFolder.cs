using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Moves a file or folder to a new location in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Moves a file or folder to a new location in OneDrive.", Kind = ActivityKind.Task)]
public class MoveFileOrFolder : OneDriveActivity<DriveItem>
{
    /// <summary>
    /// The ID or path of the item to move.
    /// </summary>
    [Input(Description = "The ID or path of the item to move.")]
    public Input<string> ItemIdOrPath { get; set; } = default!;

    /// <summary>
    /// The ID of the drive containing the item to move.
    /// </summary>
    [Input(Description = "The ID of the drive containing the item to move.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// The ID of the destination parent folder.
    /// </summary>
    [Input(Description = "The ID of the destination parent folder.")]
    public Input<string> DestinationFolderId { get; set; } = default!;

    /// <summary>
    /// The new name for the item. If not specified, the original name will be used.
    /// </summary>
    [Input(Description = "The new name for the item. If not specified, the original name will be used.")]
    public Input<string>? NewName { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var itemIdOrPath = ItemIdOrPath.Get(context);
        var destinationFolderId = DestinationFolderId.Get(context);
        var newName = NewName?.Get(context);
        var driveId = DriveId?.Get(context);

        var requestBody = new DriveItem
        {
            ParentReference = new ItemReference
            {
                Id = destinationFolderId
            }
        };

        if (!string.IsNullOrEmpty(newName))
        {
            requestBody.Name = newName;
        }

        DriveItem result;
        if (driveId != null)
        {
            // Move by ID with specified drive
            result = await graphClient.Drives[driveId].Items[itemIdOrPath].PatchAsync(requestBody, cancellationToken: context.CancellationToken);
        }
        else if (IsItemId(itemIdOrPath))
        {
            // Move by ID in default drive
            result = await graphClient.Me.Drive.Items[itemIdOrPath].PatchAsync(requestBody, cancellationToken: context.CancellationToken);
        }
        else
        {
            // Move by path in default drive
            result = await graphClient.Me.Drive.Root.ItemWithPath(itemIdOrPath).PatchAsync(requestBody, cancellationToken: context.CancellationToken);
        }

        Result.Set(context, result);
    }

    private static bool IsItemId(string value)
    {
        // Simple check to determine if the string is likely to be an ID rather than a path
        return !value.Contains('/') && !value.Contains('\\');
    }
}