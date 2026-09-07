using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Uploads a file to OneDrive from a URL.
/// Note: This is only available for OneDrive Personal.
/// </summary>
[Activity("Elsa", "OneDrive", "Uploads a file to OneDrive from a URL (Only available for OneDrive Personal).", Kind = ActivityKind.Task)]
public class UploadFileByURL : OneDriveActivity<DriveItem>
{
    private static readonly HttpClient _httpClient = new();

    /// <summary>
    /// The URL of the file to download and upload.
    /// </summary>
    [Input(Description = "The URL of the file to download and upload.")]
    public Input<string> SourceUrl { get; set; } = default!;

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
    /// Whether to overwrite an existing file with the same name.
    /// </summary>
    [Input(Description = "Whether to overwrite an existing file with the same name.")]
    public Input<bool> Overwrite { get; set; } = new(true);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var sourceUrl = SourceUrl.Get(context);
        var destinationPath = DestinationPath.Get(context);
        var folderId = FolderId?.Get(context);
        var fileName = FileName?.Get(context);
        var overwrite = Overwrite.Get(context);
        
        // Determine filename and path
        string? uploadPath = null;

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

        // Download the file from URL
        using var response = await _httpClient.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, context.CancellationToken);
        response.EnsureSuccessStatusCode();
        using var contentStream = await response.Content.ReadAsStreamAsync(context.CancellationToken);

        // Upload to OneDrive
        var graphClient = GetGraphClient(context);
        
        DriveItem result;
        if (folderId != null)
        {
            // Upload to specific folder
            result = await graphClient.Me.Drive.Items[folderId].ItemWithPath(uploadPath).Content.PutAsync(contentStream, requestConfiguration =>
            {
                requestConfiguration.Headers.Add("Prefer", overwrite ? "overwrite" : "fail");
            }, context.CancellationToken);
        }
        else
        {
            // Upload by path
            result = await graphClient.Me.Drive.Root.ItemWithPath(uploadPath).Content.PutAsync(contentStream, requestConfiguration =>
            {
                requestConfiguration.Headers.Add("Prefer", overwrite ? "overwrite" : "fail");
            }, context.CancellationToken);
        }

        Result.Set(context, result);
    }
}