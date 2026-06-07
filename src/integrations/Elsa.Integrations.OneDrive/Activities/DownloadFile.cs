using System.IO;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Downloads a file from OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Downloads a file from OneDrive.", Kind = ActivityKind.Task)]
public class DownloadFile : OneDriveActivity<Stream>
{
    /// <summary>
    /// The ID or path of the file to download.
    /// </summary>
    [Input(Description = "The ID or path of the file to download.")]
    public Input<string> FileIdOrPath { get; set; } = default!;

    /// <summary>
    /// The ID of the drive containing the file.
    /// </summary>
    [Input(Description = "The ID of the drive containing the file.")]
    public Input<string>? DriveId { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var fileIdOrPath = FileIdOrPath.Get(context);
        var driveId = DriveId?.Get(context);

        Stream content;
        if (driveId != null)
        {
            // Download by ID with specified drive
            content = await graphClient.Drives[driveId].Items[fileIdOrPath].Content.GetAsync(cancellationToken: context.CancellationToken);
        }
        else if (IsItemId(fileIdOrPath))
        {
            // Download by ID in default drive
            content = await graphClient.Me.Drive.Items[fileIdOrPath].Content.GetAsync(cancellationToken: context.CancellationToken);
        }
        else
        {
            // Download by path in default drive
            content = await graphClient.Me.Drive.Root.ItemWithPath(fileIdOrPath).Content.GetAsync(cancellationToken: context.CancellationToken);
        }
        
        // Create a memory stream to store the content
        var memoryStream = new MemoryStream();
        await content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        
        Result.Set(context, memoryStream);
    }

    private static bool IsItemId(string value)
    {
        // Simple check to determine if the string is likely to be an ID rather than a path
        return !value.Contains('/') && !value.Contains('\\');
    }
}