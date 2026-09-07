using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Deletes a file or folder from OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Deletes a file or folder from OneDrive.", Kind = ActivityKind.Task)]
public class DeleteFileOrFolder : OneDriveActivity
{
    /// <summary>
    /// The ID or path of the file or folder to delete.
    /// </summary>
    [Input(Description = "The ID or path of the file or folder to delete.")]
    public Input<string> ItemIdOrPath { get; set; } = default!;

    /// <summary>
    /// The ID of the drive containing the item to delete.
    /// </summary>
    [Input(Description = "The ID of the drive containing the item to delete.")]
    public Input<string>? DriveId { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var itemIdOrPath = ItemIdOrPath.Get(context);
        var driveId = DriveId?.Get(context);

        if (driveId != null)
        {
            // Delete by ID with specified drive
            await graphClient.Drives[driveId].Items[itemIdOrPath].DeleteAsync(cancellationToken: context.CancellationToken);
        }
        else if (IsItemId(itemIdOrPath))
        {
            // Delete by ID in default drive
            await graphClient.Me.Drive.Items[itemIdOrPath].DeleteAsync(cancellationToken: context.CancellationToken);
        }
        else
        {
            // Delete by path in default drive
            await graphClient.Me.Drive.Root.ItemWithPath(itemIdOrPath).DeleteAsync(cancellationToken: context.CancellationToken);
        }
    }

    private static bool IsItemId(string value)
    {
        // Simple check to determine if the string is likely to be an ID rather than a path
        return !value.Contains('/') && !value.Contains('\\');
    }
}