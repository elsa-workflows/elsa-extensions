using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Uploads a file to OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Uploads a file to OneDrive.", Kind = ActivityKind.Task)]
public class UploadFile : OneDriveActivity<DriveItem>
{
    /// <summary>
    /// The content of the file to upload.
    /// </summary>
    [Input(Description = "The content of the file to upload. Can be a Stream, byte[], string, or a file path.")]
    public Input<object> Content { get; set; } = default!;

    /// <summary>
    /// The path in OneDrive where the file should be uploaded, including the filename.
    /// </summary>
    [Input(Description = "The path in OneDrive where the file should be uploaded, including the filename.")]
    public Input<string> DestinationPath { get; set; } = default!;

    /// <summary>
    /// The ID of the folder to upload the file to. If specified, this is used instead of the path.
    /// </summary>
    [Input(Description = "The ID of the folder to upload the file to. If specified, this is used instead of the path.")]
    public Input<string>? FolderId { get; set; }

    /// <summary>
    /// The name of the file to create (required if using FolderId).
    /// </summary>
    [Input(Description = "The name of the file to create (required if using FolderId).")]
    public Input<string>? FileName { get; set; }

    /// <summary>
    /// The ID of the drive to upload to.
    /// </summary>
    [Input(Description = "The ID of the drive to upload to.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// Whether to overwrite an existing file with the same name.
    /// </summary>
    [Input(Description = "Whether to overwrite an existing file with the same name.")]
    public Input<bool> Overwrite { get; set; } = new(true);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var graphClient = GetGraphClient(context);
        var content = Content.Get(context);
        var destinationPath = DestinationPath.Get(context);
        var folderId = FolderId?.Get(context);
        var fileName = FileName?.Get(context);
        var driveId = DriveId?.Get(context);
        var overwrite = Overwrite.Get(context);

        // Determine filename and path
        string? uploadPath = null;
        Stream contentStream = await GetContentAsStreamAsync(content, context);

        if (folderId != null && fileName != null)
        {
            // Using folder ID and filename
            uploadPath = fileName;
        }
        else if (!string.IsNullOrEmpty(destinationPath))
        {
            // Using destination path
            uploadPath = destinationPath;
        }
        else
        {
            throw new InvalidOperationException("Either a destination path or both folderId and fileName must be provided.");
        }

        // Upload the file
        DriveItem result;
        try
        {
            if (driveId != null)
            {
                if (folderId != null)
                {
                    // Upload to specific folder in specific drive
                    result = await graphClient.Drives[driveId].Items[folderId].ItemWithPath(uploadPath).Content.PutAsync(contentStream, requestConfiguration =>
                    {
                        requestConfiguration.Headers.Add("Prefer", overwrite ? "overwrite" : "fail");
                    }, context.CancellationToken);
                }
                else
                {
                    // Upload to specific drive by path
                    result = await graphClient.Drives[driveId].Root.ItemWithPath(uploadPath).Content.PutAsync(contentStream, requestConfiguration =>
                    {
                        requestConfiguration.Headers.Add("Prefer", overwrite ? "overwrite" : "fail");
                    }, context.CancellationToken);
                }
            }
            else if (folderId != null)
            {
                // Upload to specific folder in default drive
                result = await graphClient.Me.Drive.Items[folderId].ItemWithPath(uploadPath).Content.PutAsync(contentStream, requestConfiguration =>
                {
                    requestConfiguration.Headers.Add("Prefer", overwrite ? "overwrite" : "fail");
                }, context.CancellationToken);
            }
            else
            {
                // Upload to default drive by path
                result = await graphClient.Me.Drive.Root.ItemWithPath(uploadPath).Content.PutAsync(contentStream, requestConfiguration =>
                {
                    requestConfiguration.Headers.Add("Prefer", overwrite ? "overwrite" : "fail");
                }, context.CancellationToken);
            }
        }
        finally
        {
            // Clean up the content stream
            if (content is not Stream) // Don't dispose if the original input was a stream
            {
                await contentStream.DisposeAsync();
            }
        }

        Result.Set(context, result);
    }

    private static async Task<Stream> GetContentAsStreamAsync(object content, ActivityExecutionContext context)
    {
        switch (content)
        {
            case Stream stream:
                return stream;
            
            case byte[] bytes:
                return new MemoryStream(bytes);
            
            case string str:
                if (File.Exists(str))
                {
                    // It's a file path
                    return File.OpenRead(str);
                }
                
                // It's a string content
                return new MemoryStream(Encoding.UTF8.GetBytes(str));
            
            default:
                throw new ArgumentException($"Unsupported content type: {content.GetType().Name}");
        }
    }
}