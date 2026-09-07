using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Gets metadata for a file or folder from OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Gets metadata for a file or folder from OneDrive.", Kind = ActivityKind.Task)]
public class GetFile : OneDriveActivity<DriveItem>
{
    /// <summary>
    /// The ID or path of the file or folder.
    /// </summary>
    [Input(Description = "The ID or path of the file or folder.")]
    public Input<string> ItemIdOrPath { get; set; } = default!;

    /// <summary>
    /// The ID of the drive containing the item.
    /// </summary>
    [Input(Description = "The ID of the drive containing the item.")]
    public Input<string>? DriveId { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var itemIdOrPath = ItemIdOrPath.Get(context);
        var driveId = DriveId?.Get(context);

        DriveItem result;
        if (driveId != null)
        {
            // Get by ID with specified drive
            result = await graphClient.Drives[driveId].Items[itemIdOrPath].GetAsync(cancellationToken: context.CancellationToken);
        }
        else if (IsItemId(itemIdOrPath))
        {
            // Get by ID in default drive
            result = await graphClient.Me.Drive.Items[itemIdOrPath].GetAsync(cancellationToken: context.CancellationToken);
        }
        else
        {
            // Get by path in default drive
            result = await graphClient.Me.Drive.Root.ItemWithPath(itemIdOrPath).GetAsync(cancellationToken: context.CancellationToken);
        }
        
        Result.Set(context, result);
    }

    private static bool IsItemId(string value)
    {
        // Simple check to determine if the string is likely to be an ID rather than a path
        return !value.Contains('/') && !value.Contains('\\');
    }
}