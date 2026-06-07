using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Renames a file or folder in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Renames a file or folder in OneDrive.", Kind = ActivityKind.Task)]
public class RenameFileOrFolder : OneDriveActivity<DriveItem>
{
    /// <summary>
    /// The ID or path of the item to rename.
    /// </summary>
    [Input(Description = "The ID or path of the item to rename.")]
    public Input<string> ItemIdOrPath { get; set; } = default!;

    /// <summary>
    /// The ID of the drive containing the item to rename.
    /// </summary>
    [Input(Description = "The ID of the drive containing the item to rename.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// The new name for the item.
    /// </summary>
    [Input(Description = "The new name for the item.")]
    public Input<string> NewName { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var itemIdOrPath = ItemIdOrPath.Get(context);
        var newName = NewName.Get(context);
        var driveId = DriveId?.Get(context);

        var requestBody = new DriveItem
        {
            Name = newName
        };

        DriveItem result;
        if (driveId != null)
        {
            // Rename by ID with specified drive
            result = await graphClient.Drives[driveId].Items[itemIdOrPath].PatchAsync(requestBody, cancellationToken: context.CancellationToken);
        }
        else if (IsItemId(itemIdOrPath))
        {
            // Rename by ID in default drive
            result = await graphClient.Me.Drive.Items[itemIdOrPath].PatchAsync(requestBody, cancellationToken: context.CancellationToken);
        }
        else
        {
            // Rename by path in default drive
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