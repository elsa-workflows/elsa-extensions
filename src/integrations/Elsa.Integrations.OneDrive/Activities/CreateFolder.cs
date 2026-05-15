using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Creates a new folder in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Creates a new folder in OneDrive.", Kind = ActivityKind.Task)]
public class CreateFolder : OneDriveActivity<DriveItem>
{
    /// <summary>
    /// The name of the folder to create.
    /// </summary>
    [Input(Description = "The name of the folder to create.")]
    public Input<string> FolderName { get; set; } = default!;

    /// <summary>
    /// The ID of the parent folder. If not specified, the folder will be created in the root.
    /// </summary>
    [Input(Description = "The ID of the parent folder. If not specified, the folder will be created in the root.")]
    public Input<string>? ParentFolderId { get; set; }

    /// <summary>
    /// The ID of the drive. If not specified, the folder will be created in the default drive.
    /// </summary>
    [Input(Description = "The ID of the drive. If not specified, the folder will be created in the default drive.")]
    public Input<string>? DriveId { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var folderName = FolderName.Get(context);
        var parentFolderId = ParentFolderId?.Get(context);
        var driveId = DriveId?.Get(context);

        var requestBody = new DriveItem
        {
            Name = folderName,
            Folder = new Folder(),
            AdditionalData = new Dictionary<string, object>()
            {
                { "@microsoft.graph.conflictBehavior", "rename" }
            }
        };

        DriveItem result;
        if (driveId != null)
        {
            if (parentFolderId != null)
            {
                // Create folder in a specific parent folder in a specific drive
                result = await graphClient.Drives[driveId].Items[parentFolderId].Children.PostAsync(
                    requestBody, cancellationToken: context.CancellationToken);
            }
            else
            {
                // Create folder in the root of a specific drive
                result = await graphClient.Drives[driveId].Root.Children.PostAsync(
                    requestBody, cancellationToken: context.CancellationToken);
            }
        }
        else if (parentFolderId != null)
        {
            // Create folder in a specific parent folder in the default drive
            result = await graphClient.Me.Drive.Items[parentFolderId].Children.PostAsync(
                requestBody, cancellationToken: context.CancellationToken);
        }
        else
        {
            // Create folder in the root of the default drive
            result = await graphClient.Me.Drive.Root.Children.PostAsync(
                requestBody, cancellationToken: context.CancellationToken);
        }

        Result.Set(context, result);
    }
}