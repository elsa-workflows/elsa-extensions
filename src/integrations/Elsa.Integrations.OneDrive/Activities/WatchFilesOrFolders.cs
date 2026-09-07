using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Elsa.Integrations.OneDrive.Activities;

/// <summary>
/// Watches for file or folder changes in OneDrive.
/// </summary>
[Activity("Elsa", "OneDrive", "Triggers when files or folders are created or modified in OneDrive.", Kind = ActivityKind.Trigger)]
public class WatchFilesOrFolders : Trigger
{
    /// <summary>
    /// The ID of the drive to monitor. If not specified, the user's default drive will be used.
    /// </summary>
    [Input(Description = "The ID of the drive to monitor. If not specified, the user's default drive will be used.")]
    public Input<string>? DriveId { get; set; }

    /// <summary>
    /// The ID of the folder to monitor. If not specified, the root folder will be monitored.
    /// </summary>
    [Input(Description = "The ID of the folder to monitor. If not specified, the root folder will be monitored.")]
    public Input<string>? FolderId { get; set; }

    /// <summary>
    /// Whether to watch for files, folders, or both.
    /// </summary>
    [Input(Description = "Whether to watch for files, folders, or both.")]
    public Input<string> ItemType { get; set; } = new("Both");

    /// <summary>
    /// The file extensions to watch for (e.g., '.docx', '.pdf').
    /// </summary>
    [Input(Description = "The file extensions to watch for (e.g., '.docx', '.pdf'). Leave empty to watch all files.")]
    public Input<ICollection<string>>? FileExtensions { get; set; }

    /// <summary>
    /// The polling interval in seconds.
    /// </summary>
    [Input(Description = "The polling interval in seconds.")]
    public Input<int> PollingIntervalInSeconds { get; set; } = new(60);

    /// <summary>
    /// The output item that was created or modified.
    /// </summary>
    [Output(Description = "The item that was created or modified.")]
    public Output<DriveItem> Item { get; set; } = default!;

    /// <inheritdoc />
    protected override async ValueTask TriggerAsync(ActivityExecutionContext context)
    {
        var driveId = DriveId?.Get(context);
        var folderId = FolderId?.Get(context);
        var itemType = ItemType.Get(context);
        var fileExtensions = FileExtensions?.Get(context)?.Select(ext => ext.StartsWith('.') ? ext.ToLowerInvariant() : $".{ext.ToLowerInvariant()}").ToList();
        var pollingIntervalInSeconds = PollingIntervalInSeconds.Get(context);
        var lastPolledTime = DateTimeOffset.UtcNow;
        
        var bookmarkName = GetType().Name;
        var bookmark = context.CreateBookmark(bookmarkName, Resume);
        
        // Schedule a recurring timer to poll for changes
        await context.ScheduleRecurringTimerAsync(
            TimeSpan.FromSeconds(pollingIntervalInSeconds),
            bookmark.Id,
            lastPolledTime);
    }

    private async ValueTask Resume(ActivityExecutionContext context, object? value)
    {
        // Get last polled time from the input or use current time as fallback
        var lastPolledTime = value is DateTimeOffset time ? time : DateTimeOffset.UtcNow.AddHours(-1);
        
        // Get current time for the next polling cycle
        var currentTime = DateTimeOffset.UtcNow;

        // Get the parameters
        var driveId = DriveId?.Get(context);
        var folderId = FolderId?.Get(context);
        var itemType = ItemType.Get(context)?.ToLowerInvariant();
        var fileExtensions = FileExtensions?.Get(context);
        var pollingIntervalInSeconds = PollingIntervalInSeconds.Get(context);
        
        // Create GraphServiceClient
        var graphClient = context.GetRequiredService<OneDriveClientFactory>().CreateClient();

        // Get the list of files and/or folders modified since last poll
        DriveItemCollectionResponse result;
        
        // Build filter based on item type
        string queryFilter = $"lastModifiedDateTime ge {lastPolledTime.ToString("o")}";
        if (itemType == "files")
        {
            queryFilter += " and file ne null";
        }
        else if (itemType == "folders")
        {
            queryFilter += " and folder ne null";
        }

        try
        {
            if (driveId != null)
            {
                if (folderId != null)
                {
                    // Get items from specific folder in specific drive
                    result = await graphClient.Drives[driveId].Items[folderId].Children.GetAsync(
                        requestConfiguration => requestConfiguration.QueryParameters.Filter = queryFilter,
                        cancellationToken: context.CancellationToken);
                }
                else
                {
                    // Get items from root of specific drive
                    result = await graphClient.Drives[driveId].Root.Children.GetAsync(
                        requestConfiguration => requestConfiguration.QueryParameters.Filter = queryFilter,
                        cancellationToken: context.CancellationToken);
                }
            }
            else if (folderId != null)
            {
                // Get items from specific folder in default drive
                result = await graphClient.Me.Drive.Items[folderId].Children.GetAsync(
                    requestConfiguration => requestConfiguration.QueryParameters.Filter = queryFilter,
                    cancellationToken: context.CancellationToken);
            }
            else
            {
                // Get items from root of default drive
                result = await graphClient.Me.Drive.Root.Children.GetAsync(
                    requestConfiguration => requestConfiguration.QueryParameters.Filter = queryFilter,
                    cancellationToken: context.CancellationToken);
            }

            // Filter file results by extension if required
            IEnumerable<DriveItem> filteredItems = result.Value ?? new List<DriveItem>();
            if (fileExtensions != null && fileExtensions.Any() && (itemType == "files" || itemType == "both"))
            {
                filteredItems = filteredItems.Where(item => 
                    item.Name != null && 
                    item.File != null &&
                    fileExtensions.Any(ext => item.Name.EndsWith(ext, StringComparison.OrdinalIgnoreCase))).ToList();
            }
            
            // Trigger workflow for each matching item
            foreach (var item in filteredItems)
            {
                Item.Set(context, item);
                
                // Complete the activity to continue the workflow
                await context.CompleteActivityAsync();
            }
        }
        catch (Exception ex)
        {
            context.JournalData.Add("Error", ex.Message);
        }
        
        // Schedule the next poll
        var bookmarkName = GetType().Name;
        var bookmark = context.CreateBookmark(bookmarkName, Resume);
        
        await context.ScheduleRecurringTimerAsync(
            TimeSpan.FromSeconds(pollingIntervalInSeconds),
            bookmark.Id,
            currentTime);
    }
}